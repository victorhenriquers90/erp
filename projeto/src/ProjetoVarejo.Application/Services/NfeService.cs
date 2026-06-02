using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Infrastructure.Nfce;
using ProjetoVarejo.Infrastructure.Nfe;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

/// <summary>
/// Emissão de NF-e (modelo 55, B2B) — faturamento para outra empresa.
/// Reaproveita o assinador e o cliente SEFAZ (host NF-e), sem QR Code/CSC.
/// </summary>
public class NfeService
{
    private readonly AppDbContext _db;
    private readonly NfeXmlGenerator _xmlGen;
    private readonly NfceAssinador _assinador;
    private readonly SefazSpClient _sefaz;
    private readonly Sessao.SessaoApp _sessao;

    public NfeService(AppDbContext db, NfeXmlGenerator xmlGen, NfceAssinador assinador,
        SefazSpClient sefaz, Sessao.SessaoApp sessao)
    {
        _db = db; _xmlGen = xmlGen; _assinador = assinador; _sefaz = sefaz; _sessao = sessao;
    }

    private async Task<EmpresaConfig?> ObterEmpresaAsync() =>
        _sessao.EmpresaAtiva != null
            ? await _db.EmpresaConfigs.FirstOrDefaultAsync(e => e.Id == _sessao.EmpresaAtiva.Id)
            : await _db.EmpresaConfigs.OrderBy(e => e.Id).FirstOrDefaultAsync();

    public async Task<Result<NotaFiscal>> EmitirAsync(int vendaId)
    {
        var venda = await _db.Vendas
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamentos)
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == vendaId);
        if (venda == null) return Result.Falha<NotaFiscal>("Documento não encontrado.");
        if (venda.NotaFiscalId.HasValue)
            return Result.Falha<NotaFiscal>("Documento já possui nota fiscal vinculada.");
        if (venda.Cliente == null || string.IsNullOrWhiteSpace(venda.Cliente.CpfCnpj))
            return Result.Falha<NotaFiscal>("NF-e exige um destinatário (cliente com CPF/CNPJ).");
        if (!venda.Itens.Any())
            return Result.Falha<NotaFiscal>("Inclua ao menos um item para emitir a NF-e.");

        var empresa = await ObterEmpresaAsync();
        if (empresa == null) return Result.Falha<NotaFiscal>("Configuração da empresa não encontrada.");
        if (string.IsNullOrWhiteSpace(empresa.CertificadoCaminho))
            return Result.Falha<NotaFiscal>("Certificado A1 não configurado (Filiais).");
        if (!File.Exists(empresa.CertificadoCaminho))
            return Result.Falha<NotaFiscal>($"Arquivo de certificado não encontrado: {empresa.CertificadoCaminho}");

        var nota = new NotaFiscal
        {
            VendaId = venda.Id,
            Numero = empresa.ProximoNumeroNfe,
            Serie = empresa.SerieNfe,
            Modelo = "55",
            Status = StatusNotaFiscal.EmDigitacao
        };

        string xml, chave;
        try
        {
            xml = _xmlGen.GerarXml(venda, empresa, nota.Numero, nota.Serie, out chave, out _);
            nota.ChaveAcesso = chave;
        }
        catch (Exception ex) { return Result.Falha<NotaFiscal>("Erro ao gerar XML: " + ex.Message); }

        string xmlAssinado;
        try
        {
            xmlAssinado = _assinador.Assinar(xml, empresa.CertificadoCaminho, empresa.CertificadoSenha, "NFe" + chave);
            nota.XmlEnviado = xmlAssinado;
        }
        catch (Exception ex) { return Result.Falha<NotaFiscal>("Erro ao assinar XML: " + ex.Message); }

        SefazSpClient.RetornoSefaz retorno;
        try
        {
            retorno = await _sefaz.EnviarAutorizacaoAsync(
                xmlAssinado, empresa.CertificadoCaminho, empresa.CertificadoSenha,
                empresa.AmbienteHomologacao, nota.Numero, usarNfce: false);
        }
        catch (Exception ex) { return Result.Falha<NotaFiscal>("Erro ao enviar à SEFAZ: " + ex.Message); }

        nota.XmlRetorno = retorno.XmlRetornoCompleto;
        nota.MensagemSefaz = $"[{retorno.CStat}] {retorno.XMotivo}";

        if (retorno.Autorizada)
        {
            nota.Status = StatusNotaFiscal.Autorizada;
            nota.Protocolo = retorno.NProt;
            nota.AutorizadaEm = DateTime.Now;
            empresa.ProximoNumeroNfe++;
        }
        else
        {
            nota.Status = StatusNotaFiscal.Rejeitada;
        }

        _db.NotasFiscais.Add(nota);
        await _db.SaveChangesAsync();

        if (nota.Status == StatusNotaFiscal.Autorizada)
        {
            venda.NotaFiscalId = nota.Id;
            await _db.SaveChangesAsync();
        }

        return retorno.Autorizada
            ? Result.Ok(nota)
            : Result.Falha<NotaFiscal>($"SEFAZ rejeitou: [{retorno.CStat}] {retorno.XMotivo}");
    }
}
