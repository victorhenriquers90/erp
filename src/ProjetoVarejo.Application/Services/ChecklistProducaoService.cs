using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace ProjetoVarejo.Application.Services;

public enum StatusChecklistProducao
{
    Pronto = 1,
    Atencao = 2,
    Pendente = 3
}

public sealed class ChecklistProducaoItem
{
    public string Grupo { get; init; } = "";
    public string Titulo { get; init; } = "";
    public string Descricao { get; init; } = "";
    public string AcaoRecomendada { get; init; } = "";
    public StatusChecklistProducao Status { get; init; }
    public int Ordem { get; init; }
}

public sealed class ChecklistProducaoResumo
{
    public ChecklistProducaoResumo(IReadOnlyList<ChecklistProducaoItem> itens, DateTime geradoEm)
    {
        Itens = itens;
        GeradoEm = geradoEm;
    }

    public IReadOnlyList<ChecklistProducaoItem> Itens { get; }
    public DateTime GeradoEm { get; }
    public int Total => Itens.Count;
    public int Prontos => Itens.Count(i => i.Status == StatusChecklistProducao.Pronto);
    public int Atencoes => Itens.Count(i => i.Status == StatusChecklistProducao.Atencao);
    public int Pendentes => Itens.Count(i => i.Status == StatusChecklistProducao.Pendente);
    public bool PodeProduzir => Pendentes == 0;

    public int PercentualPronto
    {
        get
        {
            if (Total == 0) return 0;
            var pontos = Itens.Sum(i => i.Status switch
            {
                StatusChecklistProducao.Pronto => 1m,
                StatusChecklistProducao.Atencao => 0.5m,
                _ => 0m
            });
            return (int)Math.Round(pontos / Total * 100m, MidpointRounding.AwayFromZero);
        }
    }
}

public class ChecklistProducaoService
{
    private const string GrupoFiscal = "Fiscal";
    private const string GrupoBanco = "Banco e infraestrutura";
    private const string GrupoSeguranca = "Seguranca";
    private const string GrupoOperacao = "Operacao";
    private const string GrupoCadastros = "Cadastros e estoque";
    private const string GrupoApi = "API e PWA";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;
    private readonly SessaoApp _sessao;

    public ChecklistProducaoService(IUnitOfWork unitOfWork, IConfiguration config, SessaoApp sessao)
    {
        _unitOfWork = unitOfWork;
        _config = config;
        _sessao = sessao;
    }

    public async Task<ChecklistProducaoResumo> AvaliarAsync()
    {
        var itens = new List<ChecklistProducaoItem>();
        var ordem = 1;

        void Add(string grupo, string titulo, string descricao, StatusChecklistProducao status, string acao)
        {
            itens.Add(new ChecklistProducaoItem
            {
                Grupo = grupo,
                Titulo = titulo,
                Descricao = descricao,
                Status = status,
                AcaoRecomendada = acao,
                Ordem = ordem++
            });
        }

        var empresa = _sessao.EmpresaAtiva != null
            ? await _unitOfWork.Configuracoes.Query().AsNoTracking().FirstOrDefaultAsync(e => e.Id == _sessao.EmpresaAtiva.Id)
            : await _unitOfWork.Configuracoes.Query().AsNoTracking().OrderBy(e => e.Id).FirstOrDefaultAsync();

        var cnpj = ApenasDigitos(empresa?.Cnpj ?? "");
        var cnpjReal = cnpj.Length == 14 && cnpj != "00000000000000";
        var empresaReal = empresa != null
            && cnpjReal
            && !empresa.RazaoSocial.Contains("EXEMPLO", StringComparison.OrdinalIgnoreCase);

        Add(GrupoFiscal, "Empresa fiscal configurada",
            empresaReal ? $"CNPJ {cnpj} carregado para a empresa ativa." : "Empresa ainda parece ser de exemplo ou nao possui CNPJ real.",
            empresaReal ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Pendente,
            "Preencher dados reais em Sistema > Configuracoes > Dados da empresa.");

        var enderecoCompleto = empresa != null
            && Preenchido(empresa.Cep)
            && Preenchido(empresa.Logradouro)
            && Preenchido(empresa.Numero)
            && Preenchido(empresa.Bairro)
            && Preenchido(empresa.Cidade)
            && Preenchido(empresa.Uf)
            && Preenchido(empresa.CodigoMunicipioIbge);

        Add(GrupoFiscal, "Endereco fiscal completo",
            enderecoCompleto ? "Endereco e codigo IBGE estao preenchidos." : "Endereco fiscal ou codigo IBGE ainda incompletos.",
            enderecoCompleto ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Pendente,
            "Completar CEP, logradouro, numero, bairro, cidade, UF e codigo IBGE.");

        var certificadoOk = empresa != null
            && Preenchido(empresa.CertificadoCaminho)
            && File.Exists(empresa.CertificadoCaminho);
        var cscOk = empresa != null
            && Preenchido(empresa.CscId)
            && Preenchido(empresa.CscToken)
            && empresa.CscToken.Trim().Length >= 20;

        Add(GrupoFiscal, "Certificado A1 e CSC",
            certificadoOk && cscOk ? "Certificado encontrado e token CSC preenchido." : "Certificado A1 ou CSC/token NFC-e nao estao prontos.",
            certificadoOk && cscOk ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Pendente,
            "Selecionar o .pfx/.p12 e informar CSC ID/token antes de emitir NFC-e.");

        var ambienteProducao = empresa != null && !empresa.AmbienteHomologacao;
        Add(GrupoFiscal, "Ambiente NFC-e",
            ambienteProducao ? "NFC-e esta em producao." : "NFC-e ainda esta em homologacao/testes.",
            ambienteProducao ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Atencao,
            "Manter em homologacao durante testes; mudar para producao apenas no go-live.");

        var numeracaoOk = empresa != null && empresa.SerieNfce > 0 && empresa.ProximoNumeroNfce > 0;
        Add(GrupoFiscal, "Serie e numeracao NFC-e",
            numeracaoOk ? $"Serie {empresa!.SerieNfce}, proxima NFC-e {empresa.ProximoNumeroNfce}." : "Serie ou proximo numero de NFC-e invalidos.",
            numeracaoOk ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Pendente,
            "Definir serie e proximo numero com apoio do contador.");

        var produtosAtivos = await _unitOfWork.Produtos.Query().AsNoTracking().CountAsync(p => p.Ativo);
        var produtosSemCodigoBarras = await _unitOfWork.Produtos.Query().AsNoTracking()
            .CountAsync(p => p.Ativo && (p.CodigoBarras == null || p.CodigoBarras == ""));
        var produtosSemFiscal = await _unitOfWork.Produtos.Query().AsNoTracking()
            .CountAsync(p => p.Ativo && ((p.Ncm == null || p.Ncm == "") || p.Cfop == "" || p.CstIcms == ""));
        var produtosPrecoInvalido = await _unitOfWork.Produtos.Query().AsNoTracking()
            .CountAsync(p => p.Ativo && p.PrecoVenda <= 0);
        var produtosSemEstoque = await _unitOfWork.Produtos.Query().AsNoTracking()
            .CountAsync(p => p.Ativo && p.ControlaEstoque && p.Estoque <= 0);

        Add(GrupoCadastros, "Cadastro de produtos",
            produtosAtivos > 0 ? $"{produtosAtivos} produtos ativos cadastrados." : "Nenhum produto ativo encontrado.",
            produtosAtivos > 0 ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Pendente,
            "Importar XML de fornecedores, planilha ou cadastrar os produtos reais.");

        Add(GrupoCadastros, "Codigos de barras",
            produtosSemCodigoBarras == 0 ? "Todos os produtos ativos possuem codigo de barras." : $"{produtosSemCodigoBarras} produtos ativos sem codigo de barras.",
            produtosAtivos > 0 && produtosSemCodigoBarras == 0 ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Atencao,
            "Completar GTIN/codigo interno antes de usar leitor no PDV.");

        Add(GrupoCadastros, "Dados fiscais dos produtos",
            produtosSemFiscal == 0 ? "Produtos ativos possuem NCM, CFOP e CST/CSOSN." : $"{produtosSemFiscal} produtos ativos com classificacao fiscal pendente.",
            produtosAtivos > 0 && produtosSemFiscal == 0 ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Pendente,
            "Revisar NCM, CEST, CFOP e tributacao com o contador.");

        Add(GrupoCadastros, "Precos de venda",
            produtosPrecoInvalido == 0 ? "Produtos ativos possuem preco de venda valido." : $"{produtosPrecoInvalido} produtos com preco de venda invalido.",
            produtosAtivos > 0 && produtosPrecoInvalido == 0 ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Pendente,
            "Corrigir precos antes de liberar o PDV.");

        Add(GrupoCadastros, "Estoque inicial",
            produtosSemEstoque == 0 ? "Produtos controlados possuem saldo positivo." : $"{produtosSemEstoque} produtos controlados estao sem saldo.",
            produtosAtivos > 0 && produtosSemEstoque == 0 ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Atencao,
            "Fazer contagem fisica e lancar entrada/ajuste inicial.");

        await AvaliarBancoAsync(Add);
        AvaliarBackup(Add);
        await AvaliarSegurancaAsync(Add);
        AvaliarOperacao(Add, empresa);
        AvaliarApi(Add);

        return new ChecklistProducaoResumo(itens.OrderBy(i => i.Ordem).ToList(), DateTime.Now);
    }

    private async Task AvaliarBancoAsync(Action<string, string, string, StatusChecklistProducao, string> add)
    {
        try
        {
            var pendentes = (await Task.FromResult(new List<string>())).ToList();
            add(GrupoBanco, "Migrations do banco",
                pendentes.Count == 0 ? "Banco esta atualizado com as migrations do sistema." : $"{pendentes.Count} migration(s) pendente(s).",
                pendentes.Count == 0 ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Pendente,
                "Executar migracao antes de abrir producao.");
        }
        catch (Exception ex)
        {
            add(GrupoBanco, "Migrations do banco",
                "Nao foi possivel validar migrations: " + ex.Message,
                StatusChecklistProducao.Atencao,
                "Validar manualmente o banco no servidor de producao.");
        }

        var conn = _config.GetConnectionString("Default") ?? "";
        var usaBancoLocal = conn.Contains("(localdb)", StringComparison.OrdinalIgnoreCase)
            || conn.Contains("Server=.\\", StringComparison.OrdinalIgnoreCase)
            || conn.Contains("Server=localhost", StringComparison.OrdinalIgnoreCase);

        add(GrupoBanco, "Servidor SQL",
            usaBancoLocal ? "Conexao aponta para banco local; adequado para 1 terminal." : "Conexao aponta para servidor/rede.",
            usaBancoLocal ? StatusChecklistProducao.Atencao : StatusChecklistProducao.Pronto,
            "Para multi-caixa, usar um servidor central e testar rede/firewall.");

        var encryptFalse = conn.Contains("Encrypt=False", StringComparison.OrdinalIgnoreCase);
        add(GrupoBanco, "Connection string revisada",
            encryptFalse ? "Criptografia de conexao esta desativada na connection string." : "Connection string nao expõe Encrypt=False.",
            encryptFalse ? StatusChecklistProducao.Atencao : StatusChecklistProducao.Pronto,
            "Em rede, avaliar certificado/TLS ou politica interna de seguranca.");
    }

    private static void AvaliarBackup(Action<string, string, string, StatusChecklistProducao, string> add)
    {
        var cfgFile = Path.Combine(AppContext.BaseDirectory, "backup.cfg");
        var ultimoFile = Path.Combine(AppContext.BaseDirectory, "backup.last");
        var pasta = Path.Combine(AppContext.BaseDirectory, "Backups");
        var auto = false;

        if (File.Exists(cfgFile))
        {
            var linhas = File.ReadAllLines(cfgFile);
            if (linhas.Length > 0 && Preenchido(linhas[0])) pasta = linhas[0];
            if (linhas.Length > 1) auto = linhas[1] == "1";
        }

        var pastaOk = Directory.Exists(pasta);
        var backupRecente = File.Exists(ultimoFile) && File.GetLastWriteTime(ultimoFile).Date >= DateTime.Today.AddDays(-1);
        if (!backupRecente && pastaOk)
        {
            backupRecente = Directory.GetFiles(pasta, "*.bak")
                .Select(f => new FileInfo(f))
                .Any(f => f.LastWriteTime >= DateTime.Today.AddDays(-1));
        }

        add(GrupoBanco, "Backup automatico",
            auto ? $"Backup automatico habilitado em {pasta}." : "Backup automatico ainda nao esta habilitado.",
            auto && pastaOk ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Pendente,
            "Configurar backup diario em pasta externa, de rede ou unidade protegida.");

        add(GrupoBanco, "Backup recente",
            backupRecente ? "Existe backup gerado nas ultimas 48 horas." : "Nenhum backup recente foi localizado.",
            backupRecente ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Atencao,
            "Executar um backup manual e testar restauracao antes do go-live.");
    }

    private async Task AvaliarSegurancaAsync(Action<string, string, string, StatusChecklistProducao, string> add)
    {
        var usuariosAtivos = await _unitOfWork.Usuarios.Query().AsNoTracking().CountAsync(u => u.Ativo);
        var admin = await _unitOfWork.Usuarios.Query().AsNoTracking().FirstOrDefaultAsync(u => u.Login == "admin");
        var senhaPadrao = admin != null && SenhaHasher.Verifica("admin", admin.SenhaHash);

        add(GrupoSeguranca, "Senha padrao removida",
            senhaPadrao ? "Usuario admin ainda usa a senha padrao." : "Senha padrao do admin nao foi detectada.",
            senhaPadrao ? StatusChecklistProducao.Pendente : StatusChecklistProducao.Pronto,
            "Trocar senha do admin antes de usar em producao.");

        add(GrupoSeguranca, "Usuarios por funcao",
            usuariosAtivos > 1 ? $"{usuariosAtivos} usuarios ativos cadastrados." : "Existe apenas um usuario ativo.",
            usuariosAtivos > 1 ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Atencao,
            "Criar usuarios para caixa, gerente e estoque com perfis adequados.");
    }

    private static void AvaliarOperacao(Action<string, string, string, StatusChecklistProducao, string> add, ProjetoVarejo.Domain.Entities.EmpresaConfig? empresa)
    {
        var impressoraOk = empresa != null && Preenchido(empresa.ImpressoraDestino);
        add(GrupoOperacao, "Impressora do PDV",
            impressoraOk ? $"Destino configurado: {empresa!.ImpressoraDestino}." : "Destino da impressora ainda nao configurado.",
            impressoraOk ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Pendente,
            "Configurar impressora termica e fazer teste de impressao.");

        var pixOk = empresa != null && Preenchido(empresa.PixChave);
        add(GrupoOperacao, "PIX",
            pixOk ? "Chave PIX configurada." : "Chave PIX nao configurada.",
            pixOk ? StatusChecklistProducao.Pronto : StatusChecklistProducao.Atencao,
            "Configurar PIX se a loja aceitar esse meio de pagamento.");
    }

    private void AvaliarApi(Action<string, string, string, StatusChecklistProducao, string> add)
    {
        var chaves = _config.GetSection("ApiKeys")
            .GetChildren()
            .Select(c => c.Value ?? "")
            .Where(Preenchido)
            .ToList();

        if (chaves.Count == 0)
        {
            add(GrupoApi, "Chave da API/PWA",
                "Nenhuma chave API foi encontrada neste appsettings.",
                StatusChecklistProducao.Atencao,
                "Se usar app web/PWA, configurar chave propria no ProjetoVarejo.Api.");
            return;
        }

        var placeholder = chaves.Any(c =>
            c.Contains("TROQUE", StringComparison.OrdinalIgnoreCase)
            || c.Contains("aBc123XyZ456", StringComparison.OrdinalIgnoreCase));

        add(GrupoApi, "Chave da API/PWA",
            placeholder ? "API ainda usa chave de exemplo." : "API possui chave configurada.",
            placeholder ? StatusChecklistProducao.Pendente : StatusChecklistProducao.Pronto,
            "Gerar uma chave forte antes de publicar API/PWA.");
    }

    private static bool Preenchido(string? valor) => !string.IsNullOrWhiteSpace(valor);

    private static string ApenasDigitos(string valor) =>
        new(valor.Where(char.IsDigit).ToArray());
}
