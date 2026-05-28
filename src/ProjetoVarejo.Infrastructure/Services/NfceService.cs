using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Domain.Enums;
using ProjetoVarejo.Shared;
using ProjetoVarejo.Infrastructure.Nfce;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ProjetoVarejo.Infrastructure.Services;

/// <summary>
/// Service for handling NFC-e (Nota Fiscal de Consumidor Eletrônica) operations.
///
/// Implements INfceService interface from Application.Contracts to enable clean architecture.
/// Resides in Infrastructure layer due to direct dependencies on NFC-e generation, signing, and SEFAZ communication.
///
/// PHASE 2.5: All database access refactored to use IUnitOfWork
/// PHASE 3: Implements INfceService interface for abstraction
/// PHASE 4: Ready for unit testing via Moq-based test infrastructure
/// </summary>
public class NfceService : INfceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SessaoApp _sessao;
    private readonly ProducaoGuardService _producaoGuard;
    private readonly NfceXmlGenerator _xmlGen;
    private readonly NfceAssinador _assinador;
    private readonly SefazSpClient _sefaz;
    private readonly NfceCancelamentoBuilder _cancelBuilder;
    private readonly NfceInutilizacaoBuilder _inutBuilder;

    public NfceService(
        IUnitOfWork unitOfWork,
        NfceXmlGenerator xmlGen,
        NfceAssinador assinador,
        SefazSpClient sefaz,
        NfceCancelamentoBuilder cancelBuilder,
        NfceInutilizacaoBuilder inutBuilder,
        SessaoApp sessao,
        ProducaoGuardService producaoGuard)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _xmlGen = xmlGen ?? throw new ArgumentNullException(nameof(xmlGen));
        _assinador = assinador ?? throw new ArgumentNullException(nameof(assinador));
        _sefaz = sefaz ?? throw new ArgumentNullException(nameof(sefaz));
        _cancelBuilder = cancelBuilder ?? throw new ArgumentNullException(nameof(cancelBuilder));
        _inutBuilder = inutBuilder ?? throw new ArgumentNullException(nameof(inutBuilder));
        _sessao = sessao ?? throw new ArgumentNullException(nameof(sessao));
        _producaoGuard = producaoGuard ?? throw new ArgumentNullException(nameof(producaoGuard));
    }

    public async Task<bool> SefazOnlineAsync()
    {
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var empresa = _sessao.EmpresaAtiva != null
                ? await _unitOfWork.Configuracoes.Query().FirstOrDefaultAsync(e => e.Id == _sessao.EmpresaAtiva.Id)
                : await _unitOfWork.Configuracoes.Query().OrderBy(e => e.Id).FirstOrDefaultAsync();
            var url = (empresa?.AmbienteHomologacao ?? true)
                ? "https://homologacao.nfce.fazenda.sp.gov.br/ws/NFeStatusServico4.asmx"
                : "https://nfce.fazenda.sp.gov.br/ws/NFeStatusServico4.asmx";
            var resp = await http.GetAsync(url);
            return resp.IsSuccessStatusCode || resp.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Falha ao verificar disponibilidade do SEFAZ-SP");
            return false;
        }
    }

    public async Task<Result<NotaFiscal>> EmitirContingenciaAsync(int vendaId)
        => await EmitirAsync(vendaId, contingencia: true);

    public async Task<List<NotaFiscal>> ListarContingenciaPendentesAsync() =>
        await _unitOfWork.NotasFiscais.Query()
            .Where(n => n.EmitidaEmContingencia && n.Status != StatusNotaFiscal.Autorizada)
            .Include(n => n.Venda)
            .OrderBy(n => n.CriadoEm)
            .ToListAsync();

    public async Task<Result<int>> ReenviarContingenciaAsync()
    {
        var pendentes = await ListarContingenciaPendentesAsync();
        int reenviadas = 0;
        var empresa = _sessao.EmpresaAtiva != null
            ? await _unitOfWork.Configuracoes.Query().FirstOrDefaultAsync(e => e.Id == _sessao.EmpresaAtiva.Id)
            : await _unitOfWork.Configuracoes.Query().OrderBy(e => e.Id).FirstOrDefaultAsync();
        if (empresa == null) return Result.Falha<int>("Empresa não configurada.");

        foreach (var nota in pendentes)
        {
            if (string.IsNullOrWhiteSpace(nota.XmlEnviado)) continue;
            try
            {
                var retorno = await _sefaz.EnviarAutorizacaoAsync(
                    nota.XmlEnviado, empresa.CertificadoCaminho, empresa.CertificadoSenha,
                    empresa.AmbienteHomologacao, nota.Numero);
                nota.MensagemSefaz = $"[{retorno.CStat}] {retorno.XMotivo}";
                nota.XmlRetorno = retorno.XmlRetornoCompleto;
                if (retorno.Autorizada)
                {
                    nota.Status = StatusNotaFiscal.Autorizada;
                    nota.Protocolo = retorno.NProt;
                    nota.AutorizadaEm = DateTime.Now;
                    nota.ReenviadaEm = DateTime.Now;
                    reenviadas++;
                }
            }
            catch (Exception ex)
            {
                nota.MensagemSefaz = "Reenvio falhou: " + ex.Message;
            }
        }
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok(reenviadas);
    }

    public async Task<Result<NotaFiscal>> EmitirAsync(int vendaId)
        => await EmitirAsync(vendaId, contingencia: false);

    private async Task<Result<NotaFiscal>> EmitirAsync(int vendaId, bool contingencia)
    {
        var prontidao = await _producaoGuard.ValidarNfceAsync(vendaId);
        if (!prontidao.PodeContinuar)
            return Result.Falha<NotaFiscal>("NFC-e bloqueada por pendencias fiscais:\n" + prontidao.FormatarBloqueios());

        var venda = await _unitOfWork.Vendas.Query()
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamentos)
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == vendaId);
        if (venda == null) return Result.Falha<NotaFiscal>("Venda não encontrada.");
        if (venda.NotaFiscalId.HasValue)
            return Result.Falha<NotaFiscal>("Venda já possui nota fiscal vinculada.");

        var empresa = _sessao.EmpresaAtiva != null
            ? await _unitOfWork.Configuracoes.Query().FirstOrDefaultAsync(e => e.Id == _sessao.EmpresaAtiva.Id)
            : await _unitOfWork.Configuracoes.Query().OrderBy(e => e.Id).FirstOrDefaultAsync();
        if (empresa == null) return Result.Falha<NotaFiscal>("Configuração da empresa não encontrada.");
        if (string.IsNullOrWhiteSpace(empresa.CertificadoCaminho))
            return Result.Falha<NotaFiscal>("Certificado A1 não configurado.");
        if (!File.Exists(empresa.CertificadoCaminho))
            return Result.Falha<NotaFiscal>($"Arquivo de certificado não encontrado: {empresa.CertificadoCaminho}");
        if (string.IsNullOrWhiteSpace(empresa.CscId) || string.IsNullOrWhiteSpace(empresa.CscToken))
            return Result.Falha<NotaFiscal>("CSC (token NFC-e) não configurado.");

        var nota = new NotaFiscal
        {
            VendaId = venda.Id,
            Numero = empresa.ProximoNumeroNfce,
            Serie = empresa.SerieNfce,
            Modelo = "65",
            Status = StatusNotaFiscal.EmDigitacao,
            EmitidaEmContingencia = contingencia
        };

        string xml;
        string chave;
        int cNF;
        try
        {
            xml = _xmlGen.GerarXml(venda, empresa, nota.Numero, nota.Serie, out chave, out cNF, contingencia);
            nota.ChaveAcesso = chave;
        }
        catch (Exception ex)
        {
            return Result.Falha<NotaFiscal>("Erro ao gerar XML: " + ex.Message);
        }

        string xmlAssinado;
        try
        {
            xmlAssinado = _assinador.Assinar(xml, empresa.CertificadoCaminho, empresa.CertificadoSenha, "NFe" + chave);
            nota.XmlEnviado = xmlAssinado;
        }
        catch (Exception ex)
        {
            return Result.Falha<NotaFiscal>("Erro ao assinar XML: " + ex.Message);
        }

        if (contingencia)
        {
            nota.Status = StatusNotaFiscal.Contingencia;
            nota.MensagemSefaz = "Emitida em contingência. Aguardando envio à SEFAZ.";
            empresa.ProximoNumeroNfce++;
            await _unitOfWork.NotasFiscais.InsertAsync(nota);
            await _unitOfWork.SaveChangesAsync();
            venda.NotaFiscalId = nota.Id;
            await _unitOfWork.SaveChangesAsync();
            return Result.Ok(nota);
        }

        SefazSpClient.RetornoSefaz retorno;
        try
        {
            retorno = await _sefaz.EnviarAutorizacaoAsync(
                xmlAssinado, empresa.CertificadoCaminho, empresa.CertificadoSenha,
                empresa.AmbienteHomologacao, nota.Numero);
        }
        catch (Exception ex)
        {
            return Result.Falha<NotaFiscal>("Erro ao enviar à SEFAZ: " + ex.Message);
        }

        nota.XmlRetorno = retorno.XmlRetornoCompleto;
        nota.MensagemSefaz = $"[{retorno.CStat}] {retorno.XMotivo}";

        if (retorno.Autorizada)
        {
            nota.Status = StatusNotaFiscal.Autorizada;
            nota.Protocolo = retorno.NProt;
            nota.AutorizadaEm = DateTime.Now;
            empresa.ProximoNumeroNfce++;
        }
        else if (retorno.Denegada)
        {
            nota.Status = StatusNotaFiscal.Rejeitada;
        }
        else
        {
            nota.Status = StatusNotaFiscal.Rejeitada;
        }

        await _unitOfWork.NotasFiscais.InsertAsync(nota);
        await _unitOfWork.SaveChangesAsync();

        if (nota.Status == StatusNotaFiscal.Autorizada)
        {
            venda.NotaFiscalId = nota.Id;
            await _unitOfWork.SaveChangesAsync();
        }

        return Result.Ok(nota);
    }

    public async Task<EmpresaConfig?> ObterEmpresaAsync() =>
        _sessao.EmpresaAtiva != null
            ? await _unitOfWork.Configuracoes.Query().FirstOrDefaultAsync(e => e.Id == _sessao.EmpresaAtiva.Id)
            : await _unitOfWork.Configuracoes.Query().OrderBy(e => e.RazaoSocial).FirstOrDefaultAsync();

    public async Task<List<EmpresaConfig>> ListarEmpresasAsync() =>
        await _unitOfWork.Configuracoes.Query().Where(e => e.Ativo).OrderBy(e => e.RazaoSocial).ToListAsync();

    public async Task<EmpresaConfig?> ObterEmpresaPorIdAsync(int id) =>
        await _unitOfWork.Configuracoes.Query().FirstOrDefaultAsync(e => e.Id == id);

    public async Task<Result> SalvarEmpresaAsync(EmpresaConfig empresa)
    {
        if (string.IsNullOrWhiteSpace(empresa.RazaoSocial))
            return Result.Falha("Razão Social é obrigatória.");
        if (string.IsNullOrWhiteSpace(empresa.Cnpj))
            return Result.Falha("CNPJ é obrigatório.");

        if (empresa.Id == 0)
            await _unitOfWork.Configuracoes.InsertAsync(empresa);
        else
        {
            empresa.AtualizadoEm = DateTime.Now;
            await _unitOfWork.Configuracoes.UpdateAsync(empresa);
        }
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<List<NotaFiscal>> ListarAsync(DateTime? de = null, DateTime? ate = null, StatusNotaFiscal? status = null)
    {
        var q = _unitOfWork.NotasFiscais.Query().Include(n => n.Venda).AsQueryable();
        if (de.HasValue) q = q.Where(n => n.CriadoEm >= de.Value);
        if (ate.HasValue) q = q.Where(n => n.CriadoEm <= ate.Value);
        if (status.HasValue) q = q.Where(n => n.Status == status.Value);
        return await q.OrderByDescending(n => n.CriadoEm).Take(500).ToListAsync();
    }

    public async Task<Result<NotaFiscal>> CancelarAsync(int notaId, string justificativa)
    {
        var nota = await _unitOfWork.NotasFiscais.Query().Include(n => n.Venda).FirstOrDefaultAsync(n => n.Id == notaId);
        if (nota == null) return Result.Falha<NotaFiscal>("Nota não encontrada.");
        if (nota.Status != StatusNotaFiscal.Autorizada)
            return Result.Falha<NotaFiscal>("Apenas notas autorizadas podem ser canceladas.");
        if (!nota.AutorizadaEm.HasValue || (DateTime.Now - nota.AutorizadaEm.Value).TotalHours > 24)
            return Result.Falha<NotaFiscal>("Prazo de 24h para cancelamento expirado. Use Carta de Correção ou Substituição.");

        var empresa = _sessao.EmpresaAtiva != null
            ? await _unitOfWork.Configuracoes.Query().FirstOrDefaultAsync(e => e.Id == _sessao.EmpresaAtiva.Id)
            : await _unitOfWork.Configuracoes.Query().OrderBy(e => e.Id).FirstOrDefaultAsync();
        if (empresa == null) return Result.Falha<NotaFiscal>("Empresa não configurada.");
        if (string.IsNullOrWhiteSpace(empresa.CertificadoCaminho) || !File.Exists(empresa.CertificadoCaminho))
            return Result.Falha<NotaFiscal>("Certificado A1 não disponível.");

        string xmlEvento;
        string idEvento;
        try
        {
            xmlEvento = _cancelBuilder.GerarXmlEvento(nota, empresa, justificativa, 1, out idEvento);
        }
        catch (Exception ex) { return Result.Falha<NotaFiscal>("Erro ao gerar XML: " + ex.Message); }

        string assinado;
        try
        {
            assinado = _assinador.Assinar(xmlEvento, empresa.CertificadoCaminho, empresa.CertificadoSenha, idEvento);
        }
        catch (Exception ex) { return Result.Falha<NotaFiscal>("Erro ao assinar: " + ex.Message); }

        var envelopeLote = _cancelBuilder.EnveloparLote(assinado, 1);

        SefazSpClient.RetornoSefaz retorno;
        try
        {
            retorno = await _sefaz.EnviarEventoAsync(envelopeLote, empresa.CertificadoCaminho, empresa.CertificadoSenha, empresa.AmbienteHomologacao);
        }
        catch (Exception ex) { return Result.Falha<NotaFiscal>("Erro de comunicação: " + ex.Message); }

        var sucesso = retorno.CStat == "135" || retorno.CStat == "136" || retorno.CStat == "155";

        nota.MensagemSefaz = $"[{retorno.CStat}] {retorno.XMotivo}";
        if (sucesso)
        {
            nota.Status = StatusNotaFiscal.Cancelada;
            nota.CanceladaEm = DateTime.Now;
            nota.JustificativaCancelamento = justificativa;
            if (nota.Venda != null && nota.Venda.Status == StatusVenda.Finalizada)
            {
                var resCancel = await CancelarVendaInternaAsync(nota.Venda, "Cancelamento NFC-e: " + justificativa);
                if (!resCancel.Sucesso) return Result.Falha<NotaFiscal>(resCancel.Erro!);
            }
        }
        await _unitOfWork.SaveChangesAsync();

        return sucesso
            ? Result.Ok(nota)
            : Result.Falha<NotaFiscal>($"SEFAZ rejeitou: [{retorno.CStat}] {retorno.XMotivo}");
    }

    private async Task<Result> CancelarVendaInternaAsync(Venda venda, string motivo)
    {
        var vendaCarregada = await _unitOfWork.Vendas.Query()
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstAsync(v => v.Id == venda.Id);

        foreach (var item in vendaCarregada.Itens)
        {
            if (item.Produto.ControlaEstoque)
            {
                item.Produto.Estoque += item.Quantidade;
                await _unitOfWork.MovimentosEstoque.InsertAsync(new MovimentoEstoque
                {
                    ProdutoId = item.ProdutoId,
                    Tipo = TipoMovimentoEstoque.Devolucao,
                    Quantidade = item.Quantidade,
                    SaldoAnterior = item.Produto.Estoque - item.Quantidade,
                    SaldoAtual = item.Produto.Estoque,
                    Documento = $"CANCEL NFCe VENDA {vendaCarregada.Numero}",
                    VendaId = vendaCarregada.Id,
                    UsuarioId = vendaCarregada.UsuarioId,
                    Observacao = motivo
                });
            }
        }
        vendaCarregada.Status = StatusVenda.Cancelada;
        vendaCarregada.CanceladaEm = DateTime.Now;
        vendaCarregada.Observacao = motivo;
        return Result.Ok();
    }

    public async Task<Result<string>> InutilizarFaixaAsync(int serie, int nNFIni, int nNFFin, string justificativa)
    {
        var empresa = _sessao.EmpresaAtiva != null
            ? await _unitOfWork.Configuracoes.Query().FirstOrDefaultAsync(e => e.Id == _sessao.EmpresaAtiva.Id)
            : await _unitOfWork.Configuracoes.Query().OrderBy(e => e.Id).FirstOrDefaultAsync();
        if (empresa == null) return Result.Falha<string>("Empresa não configurada.");
        if (string.IsNullOrWhiteSpace(empresa.CertificadoCaminho) || !File.Exists(empresa.CertificadoCaminho))
            return Result.Falha<string>("Certificado A1 não disponível.");

        var emitidas = await _unitOfWork.NotasFiscais.Query()
            .Where(n => n.Serie == serie && n.Numero >= nNFIni && n.Numero <= nNFFin
                     && (n.Status == StatusNotaFiscal.Autorizada || n.Status == StatusNotaFiscal.Cancelada))
            .CountAsync();
        if (emitidas > 0)
            return Result.Falha<string>($"Há {emitidas} nota(s) emitida(s) na faixa. Inutilização só permitida para números nunca usados.");

        string xml;
        string idInut;
        try
        {
            xml = _inutBuilder.GerarXml(empresa, serie, nNFIni, nNFFin, justificativa, out idInut);
        }
        catch (Exception ex) { return Result.Falha<string>("Erro ao gerar XML: " + ex.Message); }

        string assinado;
        try
        {
            assinado = _assinador.Assinar(xml, empresa.CertificadoCaminho, empresa.CertificadoSenha, idInut);
        }
        catch (Exception ex) { return Result.Falha<string>("Erro ao assinar: " + ex.Message); }

        SefazSpClient.RetornoSefaz retorno;
        try
        {
            retorno = await _sefaz.EnviarInutilizacaoAsync(assinado, empresa.CertificadoCaminho, empresa.CertificadoSenha, empresa.AmbienteHomologacao);
        }
        catch (Exception ex) { return Result.Falha<string>("Erro de comunicação: " + ex.Message); }

        if (retorno.CStat == "102")
        {
            if (empresa.ProximoNumeroNfce <= nNFFin)
            {
                empresa.ProximoNumeroNfce = nNFFin + 1;
                await _unitOfWork.SaveChangesAsync();
            }
            return Result.Ok($"Inutilização homologada. Protocolo: {retorno.NProt}");
        }
        return Result.Falha<string>($"SEFAZ rejeitou: [{retorno.CStat}] {retorno.XMotivo}");
    }
}
