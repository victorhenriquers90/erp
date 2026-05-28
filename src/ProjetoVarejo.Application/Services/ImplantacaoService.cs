using System.Text.Json;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Application.Services;

public sealed class ImplantacaoConfig
{
    public TipoNegocio Perfil { get; set; } = TipoNegocio.Industria;
    public ModuloSistema ModulosAtivos { get; set; } = ModuloSistema.PDV | ModuloSistema.Estoque;
    public DateTime AtualizadoEm { get; set; } = DateTime.Now;
}

public sealed record PerfilSistemaInfo(TipoNegocio Id, string Nome, string Descricao);

public sealed record ModuloSistemaInfo(ModuloSistema Id, string Nome, string Descricao, bool Essencial = false);

public class ImplantacaoService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    // Garante que leitura e escrita nunca ocorram simultaneamente no mesmo arquivo
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly string _arquivo;

    public ImplantacaoService(string? arquivo = null)
    {
        _arquivo = string.IsNullOrWhiteSpace(arquivo) ? ArquivoPadrao() : arquivo;
    }

    public IReadOnlyList<PerfilSistemaInfo> Perfis { get; } = new List<PerfilSistemaInfo>
    {
        new(TipoNegocio.Padaria, "Padaria", "Balcao, caixa, estoque, compras e controle fiscal."),
        new(TipoNegocio.Acougue, "Acougue", "Venda por peso, estoque, compras e rotina fiscal."),
        new(TipoNegocio.Loja, "Loja", "Operacao comercial com venda direta, estoque e financeiro."),
        new(TipoNegocio.Industria, "Industria", "Foco em suprimentos, estoque, fiscal e financeiro."),
        new(TipoNegocio.Bazar, "Bazar", "Venda de variedades com cadastro simples e estoque."),
        new(TipoNegocio.Supermercado, "Supermercado", "Operacao completa de varejo alimentar."),
        new(TipoNegocio.Farmacia, "Farmacia", "Vendas de medicamentos com controle fiscal."),
        new(TipoNegocio.Restaurante, "Restaurante", "Controle de menu, comandas e servicos.")
    };

    public IReadOnlyList<ModuloSistemaInfo> ModulosDisponiveis { get; } = new List<ModuloSistemaInfo>
    {
        new(ModuloSistema.PDV, "PDV", "Frente de caixa e venda balcao."),          // Opcional: depende do segmento
        new(ModuloSistema.Estoque, "Estoque", "Entradas, saidas, inventario e alertas.", true),
        new(ModuloSistema.Cadastros, "Cadastros", "Produtos, clientes e fornecedores.", true),
        new(ModuloSistema.Financeiro, "Financeiro", "Contas a pagar, receber e quitacoes.", true),
        new(ModuloSistema.Fiscal, "Fiscal", "Notas fiscais, NFC-e e documentos fiscais."),
        new(ModuloSistema.Producao, "Producao", "Controle de producao e processos."),
        new(ModuloSistema.Prevenda, "Pre-venda", "Promocoes e pre-vendas."),
        new(ModuloSistema.Pesagem, "Pesagem", "Integracao com balancas."),
        new(ModuloSistema.Comissoes, "Comissoes", "Comissoes de vendedores."),
        new(ModuloSistema.Relatorios, "Relatorios", "Indicadores, analises e relatorios gerenciais.", true),
        new(ModuloSistema.Auditoria, "Auditoria", "Rastreamento de alteracoes e eventos.", true),
        new(ModuloSistema.Backup, "Backup", "Rotina de copia e protecao de dados.", true),
        new(ModuloSistema.Pix, "PIX", "Integracao com sistema PIX."),
        new(ModuloSistema.Tef, "TEF", "Transferencia eletronica de fundos."),
        new(ModuloSistema.Receitas, "Receitas", "Gestao de receitas (farmacia)."),
        new(ModuloSistema.Comandas, "Comandas", "Controle de comandas (restaurante).")
    };

    public async Task<ImplantacaoConfig> ObterAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (File.Exists(_arquivo))
            {
                await using var stream = new FileStream(
                    _arquivo, FileMode.Open, FileAccess.Read, FileShare.Read);
                var config = await JsonSerializer.DeserializeAsync<ImplantacaoConfig>(stream, JsonOptions);
                if (config != null)
                    return Normalizar(config);
            }
        }
        catch
        {
            // Falha de leitura nao pode impedir a abertura do sistema.
        }
        finally
        {
            _semaphore.Release();
        }

        return Normalizar(new ImplantacaoConfig
        {
            Perfil = TipoNegocio.Industria,
            ModulosAtivos = ModulosRecomendados(TipoNegocio.Industria)
        });
    }

    public async Task SalvarAsync(ImplantacaoConfig config)
    {
        var normalizado = Normalizar(config);
        normalizado.AtualizadoEm = DateTime.Now;

        var pasta = Path.GetDirectoryName(_arquivo);
        if (!string.IsNullOrWhiteSpace(pasta))
            Directory.CreateDirectory(pasta);

        await _semaphore.WaitAsync();
        try
        {
            // Escreve em arquivo temporário e move atomicamente (MOVEFILE_REPLACE_EXISTING).
            // File.Move(overwrite:true) funciona mesmo quando o destino ainda não existe
            // (primeira execução) e evita o IOException que File.Replace lança nesses casos.
            var temp = _arquivo + ".tmp";
            await using (var stream = new FileStream(
                temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, normalizado, JsonOptions);
            }
            File.Move(temp, _arquivo, overwrite: true);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ModuloSistema ModulosRecomendados(TipoNegocio perfil)
    {
        return ProjetoVarejo.Application.Configuracao.ModulosPorTipo.ObterModulosRecomendados(perfil);
    }

    public bool ModuloAtivo(ImplantacaoConfig config, ModuloSistema modulo)
    {
        var normalizado = Normalizar(config);
        return (normalizado.ModulosAtivos & modulo) == modulo;
    }

    public string NomePerfil(TipoNegocio perfil) =>
        Perfis.FirstOrDefault(p => p.Id == perfil)?.Nome ?? perfil.ToString();

    private ImplantacaoConfig Normalizar(ImplantacaoConfig config)
    {
        if (!Enum.IsDefined(config.Perfil))
            config.Perfil = TipoNegocio.Industria;

        if (config.ModulosAtivos == 0)
            config.ModulosAtivos = ModulosRecomendados(config.Perfil);

        // Garantir que módulos base (não PDV) estejam sempre ativos
        var essenciais = ModulosDisponiveis
            .Where(m => m.Essencial)
            .Select(m => m.Id)
            .Aggregate((ModuloSistema)0, (a, b) => a | b);

        config.ModulosAtivos |= essenciais;

        return config;
    }

    private static string ArquivoPadrao()
    {
        var pasta = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ProjetoVarejo");
        return Path.Combine(pasta, "implantacao.json");
    }
}
