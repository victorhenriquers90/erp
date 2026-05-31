using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ProjetoVarejo.Application.Services;

public enum SeveridadeProntidao
{
    Aviso = 1,
    Bloqueio = 2
}

public sealed class PendenciaProducao
{
    public string Titulo { get; init; } = "";
    public string Mensagem { get; init; } = "";
    public string Acao { get; init; } = "";
    public SeveridadeProntidao Severidade { get; init; }
}

public sealed class ResultadoProntidaoProducao
{
    public bool AmbienteFiscalProducao { get; init; }
    public IReadOnlyList<PendenciaProducao> Pendencias { get; init; } = Array.Empty<PendenciaProducao>();
    public IReadOnlyList<PendenciaProducao> Bloqueios => Pendencias.Where(p => p.Severidade == SeveridadeProntidao.Bloqueio).ToList();
    public IReadOnlyList<PendenciaProducao> Avisos => Pendencias.Where(p => p.Severidade == SeveridadeProntidao.Aviso).ToList();
    public bool PodeContinuar => !Bloqueios.Any();

    public string FormatarBloqueios(int limite = 8) => Formatar(Bloqueios, limite);
    public string FormatarAvisos(int limite = 8) => Formatar(Avisos, limite);

    private static string Formatar(IReadOnlyList<PendenciaProducao> itens, int limite)
    {
        if (itens.Count == 0) return "";
        var linhas = itens.Take(limite)
            .Select(p => "- " + p.Titulo + ": " + p.Mensagem)
            .ToList();
        if (itens.Count > limite)
            linhas.Add($"- mais {itens.Count - limite} pendencia(s)...");
        return string.Join(Environment.NewLine, linhas);
    }
}

public class ProducaoGuardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly SessaoApp _sessao;

    public ProducaoGuardService(IUnitOfWork unitOfWork, IConfiguration config, SessaoApp sessao)
    {
        _unitOfWork = unitOfWork;
        _config = config;
        _sessao = sessao;
    }

    public async Task<ResultadoProntidaoProducao> ValidarPdvAsync()
    {
        var pendencias = new List<PendenciaProducao>();
        var empresa = await ObterEmpresaAsync();
        var ambienteProducao = empresa != null && !empresa.AmbienteHomologacao;
        var severidadeCritica = ambienteProducao ? SeveridadeProntidao.Bloqueio : SeveridadeProntidao.Aviso;

        void Add(bool condicao, string titulo, string mensagem, string acao, SeveridadeProntidao severidade)
        {
            if (!condicao) return;
            pendencias.Add(new PendenciaProducao
            {
                Titulo = titulo,
                Mensagem = mensagem,
                Acao = acao,
                Severidade = severidade
            });
        }

        if (empresa == null)
        {
            Add(true, "Empresa nao configurada", "Nao existe empresa ativa/configurada.", "Configure os dados da empresa.", SeveridadeProntidao.Bloqueio);
            return new ResultadoProntidaoProducao { AmbienteFiscalProducao = false, Pendencias = pendencias };
        }

        var cnpj = ApenasDigitos(empresa.Cnpj);
        var empresaExemplo = cnpj != "00000000000000"
            ? false
            : true;
        empresaExemplo |= empresa.RazaoSocial.Contains("EXEMPLO", StringComparison.OrdinalIgnoreCase);

        Add(ambienteProducao && empresaExemplo, "Empresa de exemplo",
            "O PDV esta em producao fiscal, mas a empresa ainda parece ser de exemplo.",
            "Preencha CNPJ, IE e razao social reais.", SeveridadeProntidao.Bloqueio);

        Add(ambienteProducao && (!CertificadoOk(empresa) || !CscOk(empresa)), "Fiscal incompleto",
            "Certificado A1 ou CSC/token NFC-e nao estao prontos.",
            "Configure certificado A1 e CSC antes de vender em producao.", SeveridadeProntidao.Bloqueio);

        var produtosAtivos = await _unitOfWork.Produtos.Query().AsNoTracking().CountAsync(p => p.Ativo);
        var produtosSemFiscal = await _unitOfWork.Produtos.Query().AsNoTracking()
            .CountAsync(p => p.Ativo && (p.Ncm == null || p.Ncm == "" || p.Cfop == "" || p.CstIcms == "" || p.CstPisCofins == ""));
        var produtosPrecoInvalido = await _unitOfWork.Produtos.Query().AsNoTracking()
            .CountAsync(p => p.Ativo && p.PrecoVenda <= 0);

        Add(produtosAtivos == 0, "Sem produtos",
            "Nao ha produtos ativos para vender.",
            "Cadastre ou importe produtos reais.", SeveridadeProntidao.Bloqueio);

        Add(produtosSemFiscal > 0, "Produtos sem dados fiscais",
            $"{produtosSemFiscal} produto(s) ativo(s) estao com NCM/CFOP/CST pendentes.",
            "Revise classificacao fiscal com o contador.", severidadeCritica);

        Add(produtosPrecoInvalido > 0, "Produtos sem preco",
            $"{produtosPrecoInvalido} produto(s) ativo(s) estao sem preco de venda valido.",
            "Corrija precos antes de liberar o PDV.", SeveridadeProntidao.Bloqueio);

        var admin = await _unitOfWork.Usuarios.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Login == "admin");
        var senhaPadrao = admin != null && SenhaHasher.Verifica("admin", admin.SenhaHash);
        Add(senhaPadrao, "Senha padrao",
            "O usuario admin ainda usa a senha padrao.",
            "Troque a senha em Sistema > Usuarios.", severidadeCritica);

        Add(!Preenchido(empresa.ImpressoraDestino), "Impressora nao configurada",
            "A impressora do PDV ainda nao possui destino configurado.",
            "Configure e teste a impressora em Sistema > Configuracoes.", SeveridadeProntidao.Aviso);

        Add(!BackupAutomaticoConfigurado(), "Backup automatico",
            "Backup automatico diario ainda nao esta habilitado.",
            "Configure backup em Sistema > Backup.", SeveridadeProntidao.Aviso);

        return new ResultadoProntidaoProducao
        {
            AmbienteFiscalProducao = ambienteProducao,
            Pendencias = pendencias
        };
    }

    public async Task<ResultadoProntidaoProducao> ValidarNfceAsync(int vendaId)
    {
        var pendencias = new List<PendenciaProducao>();
        var empresa = await ObterEmpresaAsync();
        var ambienteProducao = empresa != null && !empresa.AmbienteHomologacao;

        void Bloquear(string titulo, string mensagem, string acao)
        {
            pendencias.Add(new PendenciaProducao
            {
                Titulo = titulo,
                Mensagem = mensagem,
                Acao = acao,
                Severidade = SeveridadeProntidao.Bloqueio
            });
        }

        if (empresa == null)
        {
            Bloquear("Empresa nao configurada", "Nao existe empresa fiscal configurada.", "Configure os dados da empresa.");
            return new ResultadoProntidaoProducao { AmbienteFiscalProducao = false, Pendencias = pendencias };
        }

        var cnpj = ApenasDigitos(empresa.Cnpj);
        if (ambienteProducao && (cnpj.Length != 14 || cnpj == "00000000000000"))
            Bloquear("CNPJ invalido", "CNPJ real e obrigatorio para NFC-e em producao.", "Revise os dados da empresa.");

        if (!CertificadoOk(empresa))
            Bloquear("Certificado A1", "Arquivo do certificado A1 nao foi localizado.", "Configure o .pfx/.p12 correto.");

        if (!CscOk(empresa))
            Bloquear("CSC NFC-e", "CSC ID/token nao estao preenchidos corretamente.", "Informe o CSC da SEFAZ.");

        if (empresa.SerieNfce <= 0 || empresa.ProximoNumeroNfce <= 0)
            Bloquear("Numeracao NFC-e", "Serie ou proximo numero da NFC-e estao invalidos.", "Revise a numeracao fiscal.");

        var venda = await _unitOfWork.Vendas.Query()
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == vendaId);

        if (venda == null)
        {
            Bloquear("Venda nao encontrada", "Nao foi possivel localizar a venda.", "Reabra o PDV e tente novamente.");
            return new ResultadoProntidaoProducao { AmbienteFiscalProducao = ambienteProducao, Pendencias = pendencias };
        }

        if (!venda.Itens.Any())
            Bloquear("Venda sem itens", "A NFC-e precisa de ao menos um item.", "Inclua produtos na venda.");

        var itensSemNcm = venda.Itens.Count(i => !NcmValido(i.Produto.Ncm));
        var itensSemCfop = venda.Itens.Count(i => !CfopValido(i.Produto.Cfop));
        var itensSemIcms = venda.Itens.Count(i => !Preenchido(i.Produto.CstIcms));
        var itensSemPisCofins = venda.Itens.Count(i => !Preenchido(i.Produto.CstPisCofins));
        var itensSemOrigem = venda.Itens.Count(i => !Preenchido(i.Produto.Origem));
        var itensPrecoInvalido = venda.Itens.Count(i => i.PrecoUnitario <= 0 || i.Total <= 0);

        if (itensSemNcm > 0)
            Bloquear("NCM pendente", $"{itensSemNcm} item(ns) da venda estao sem NCM valido.", "Corrija o cadastro fiscal dos produtos.");
        if (itensSemCfop > 0)
            Bloquear("CFOP pendente", $"{itensSemCfop} item(ns) da venda estao sem CFOP valido.", "Corrija o CFOP dos produtos.");
        if (itensSemIcms > 0)
            Bloquear("ICMS pendente", $"{itensSemIcms} item(ns) da venda estao sem CST/CSOSN ICMS.", "Corrija a tributacao ICMS.");
        if (itensSemPisCofins > 0)
            Bloquear("PIS/COFINS pendente", $"{itensSemPisCofins} item(ns) da venda estao sem CST PIS/COFINS.", "Corrija a tributacao PIS/COFINS.");
        if (itensSemOrigem > 0)
            Bloquear("Origem pendente", $"{itensSemOrigem} item(ns) da venda estao sem origem fiscal.", "Informe origem da mercadoria.");
        if (itensPrecoInvalido > 0)
            Bloquear("Valores invalidos", $"{itensPrecoInvalido} item(ns) da venda possuem preco ou total invalido.", "Revise a venda antes de emitir.");

        return new ResultadoProntidaoProducao
        {
            AmbienteFiscalProducao = ambienteProducao,
            Pendencias = pendencias
        };
    }

    private async Task<EmpresaConfig?> ObterEmpresaAsync() =>
        _sessao.EmpresaAtiva != null
            ? await _unitOfWork.Configuracoes.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == _sessao.EmpresaAtiva.Id)
            : await _unitOfWork.Configuracoes.Query().AsNoTracking().OrderBy(e => e.Id).FirstOrDefaultAsync();

    private static bool BackupAutomaticoConfigurado()
    {
        var cfgFile = Path.Combine(AppContext.BaseDirectory, "backup.cfg");
        if (!File.Exists(cfgFile)) return false;
        var linhas = File.ReadAllLines(cfgFile);
        return linhas.Length > 1 && linhas[1] == "1" && linhas.Length > 0 && Preenchido(linhas[0]);
    }

    private static bool CertificadoOk(EmpresaConfig empresa) =>
        Preenchido(empresa.CertificadoCaminho) && File.Exists(empresa.CertificadoCaminho);

    private static bool CscOk(EmpresaConfig empresa) =>
        Preenchido(empresa.CscId) && Preenchido(empresa.CscToken) && empresa.CscToken.Trim().Length >= 20;

    private static bool NcmValido(string? ncm)
    {
        var digitos = ApenasDigitos(ncm ?? "");
        return digitos.Length == 8;
    }

    private static bool CfopValido(string? cfop)
    {
        var digitos = ApenasDigitos(cfop ?? "");
        return digitos.Length == 4;
    }

    private static bool Preenchido(string? valor) => !string.IsNullOrWhiteSpace(valor);

    private static string ApenasDigitos(string valor) =>
        new(valor.Where(char.IsDigit).ToArray());
}
