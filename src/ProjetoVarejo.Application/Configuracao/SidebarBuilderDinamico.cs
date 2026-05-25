using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Application.Configuracao;

/// <summary>
/// Constrói seções da sidebar baseado nos módulos ativos.
/// Nota: Retorna dados dinâmicos para evitar dependência circular com Desktop.
/// </summary>
public class SidebarBuilderDinamico
{
    private readonly ModuloSistema _modulosAtivos;
    private readonly Type _tipoFormPdv;
    private readonly Type _tipoFormCaixa;
    private readonly Type _tipoFormNotasFiscais;
    private readonly Type _tipoFormProdutos;
    private readonly Type _tipoFormClientes;
    private readonly Type _tipoFormFornecedores;
    private readonly Type _tipoFormEstoque;
    private readonly Type _tipoFormImportarNfe;
    private readonly Type _tipoFormFinanceiro;
    private readonly Type _tipoFormRelatorios;
    private readonly Type _tipoFormBackup;
    private readonly Type _tipoFormAuditoria;
    private readonly Type _tipoFormChecklistProducao;
    private readonly Type _tipoFormConfigEmpresa;

    public SidebarBuilderDinamico(ModuloSistema modulosAtivos)
    {
        _modulosAtivos = modulosAtivos;

        // Resolver tipos de formulários
        var assembly = System.Reflection.Assembly.Load("ProjetoVarejo.Desktop");
        _tipoFormPdv = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmPdv")!;
        _tipoFormCaixa = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmCaixa")!;
        _tipoFormNotasFiscais = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmNotasFiscais")!;
        _tipoFormProdutos = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmProdutos")!;
        _tipoFormClientes = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmClientes")!;
        _tipoFormFornecedores = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmFornecedores")!;
        _tipoFormEstoque = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmEstoque")!;
        _tipoFormImportarNfe = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmImportarNfe")!;
        _tipoFormFinanceiro = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmFinanceiro")!;
        _tipoFormRelatorios = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmRelatorios")!;
        _tipoFormBackup = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmBackup")!;
        _tipoFormAuditoria = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmAuditoria")!;
        _tipoFormChecklistProducao = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmChecklistProducao")!;
        _tipoFormConfigEmpresa = assembly.GetType("ProjetoVarejo.Desktop.Forms.FrmConfigEmpresa")!;
    }

    /// <summary>
    /// Constrói as seções da sidebar conforme os módulos ativos.
    /// Retorna lista de objetos dinâmicos (compatíveis com SidebarSecao do Desktop).
    /// </summary>
    public List<dynamic> Construir()
    {
        var secoes = new List<dynamic>();

        // === Seção Principal (sempre presente) ===
        secoes.Add(new
        {
            Titulo = "Principal",
            Itens = new List<dynamic> { new { Icone = "home", Texto = "Cockpit", OnClick = (Action)(() => { }) } }
        });

        // === Seção Vendas (PDV, Caixa, Notas sempre presentes) ===
        var itensVendas = new List<dynamic>
        {
            new { Icone = "vendas", Texto = "PDV", OnClick = (Action)(() => AbrirFormulario(_tipoFormPdv)) },
            new { Icone = "caixa", Texto = "Caixa", OnClick = (Action)(() => AbrirFormulario(_tipoFormCaixa)) }
        };

        if (EstaModuloAtivo(ModuloSistema.Fiscal))
        {
            itensVendas.Add(new { Icone = "notas", Texto = "Notas Fiscais", OnClick = (Action)(() => AbrirFormulario(_tipoFormNotasFiscais)) });
        }

        if (itensVendas.Count > 0)
        {
            secoes.Add(new { Titulo = "Vendas", Itens = itensVendas });
        }

        // === Seção Cadastros (sempre presente) ===
        secoes.Add(new
        {
            Titulo = "Cadastros",
            Itens = new List<dynamic>
            {
                new { Icone = "produtos", Texto = "Produtos", OnClick = (Action)(() => AbrirFormulario(_tipoFormProdutos)) },
                new { Icone = "clientes", Texto = "Clientes", OnClick = (Action)(() => AbrirFormulario(_tipoFormClientes)) },
                new { Icone = "fornecedores", Texto = "Fornecedores", OnClick = (Action)(() => AbrirFormulario(_tipoFormFornecedores)) }
            }
        });

        // === Seção Suprimentos (Estoque + opcional Importar NF-e) ===
        var itensSuprimentos = new List<dynamic>
        {
            new { Icone = "estoque", Texto = "Estoque", OnClick = (Action)(() => AbrirFormulario(_tipoFormEstoque)) }
        };

        if (EstaModuloAtivo(ModuloSistema.Fiscal))
        {
            itensSuprimentos.Add(new { Icone = "upload", Texto = "Importar NF-e", OnClick = (Action)(() => AbrirFormulario(_tipoFormImportarNfe)) });
        }

        secoes.Add(new { Titulo = "Suprimentos", Itens = itensSuprimentos });

        // === Seção Gestão (Financeiro + Relatórios - sempre presentes) ===
        var itensGestao = new List<dynamic>
        {
            new { Icone = "financeiro", Texto = "Financeiro", OnClick = (Action)(() => AbrirFormulario(_tipoFormFinanceiro)) },
            new { Icone = "relatorios", Texto = "Relatórios", OnClick = (Action)(() => AbrirFormulario(_tipoFormRelatorios)) }
        };

        secoes.Add(new { Titulo = "Gestão", Itens = itensGestao });

        // === Seção Sistema (Backup + Auditoria + Checklist sempre presente) ===
        var itensSistema = new List<dynamic>
        {
            new { Icone = "backup", Texto = "Backup", OnClick = (Action)(() => AbrirFormulario(_tipoFormBackup)) },
            new { Icone = "auditoria", Texto = "Auditoria", OnClick = (Action)(() => AbrirFormulario(_tipoFormAuditoria)) },
            new { Icone = "checklist", Texto = "Checklist de Producao", OnClick = (Action)(() => AbrirFormulario(_tipoFormChecklistProducao)) },
            new { Icone = "config", Texto = "Configurações", OnClick = (Action)(() => AbrirFormulario(_tipoFormConfigEmpresa)) }
        };

        secoes.Add(new { Titulo = "Sistema", Itens = itensSistema });

        return secoes;
    }

    /// <summary>
    /// Obtém as seções da sidebar com indicadores de módulos.
    /// </summary>
    public IEnumerable<(string Titulo, List<string> ItensAtivos, List<string> ItensBloqueados)> AnalisisarSecoes()
    {
        var secoes = new List<(string, List<string>, List<string>)>();

        // Mapeamento: Seção -> (Módulo, Nome Item)
        var mapa = new Dictionary<string, List<(ModuloSistema?, string)>>
        {
            ["Vendas"] = new()
            {
                (ModuloSistema.PDV, "PDV"),
                (ModuloSistema.PDV, "Caixa"),
                (ModuloSistema.Fiscal, "Notas Fiscais")
            },
            ["Suprimentos"] = new()
            {
                (ModuloSistema.Estoque, "Estoque"),
                (ModuloSistema.Fiscal, "Importar NF-e")
            },
            ["Gestão"] = new()
            {
                (ModuloSistema.Financeiro, "Financeiro"),
                (null, "Relatórios") // Sempre visível
            }
        };

        foreach (var (secao, itens) in mapa)
        {
            var ativos = new List<string>();
            var bloqueados = new List<string>();

            foreach (var (modulo, nome) in itens)
            {
                if (modulo == null || EstaModuloAtivo(modulo.Value))
                    ativos.Add(nome);
                else
                    bloqueados.Add(nome);
            }

            if (ativos.Count > 0 || bloqueados.Count > 0)
                secoes.Add((secao, ativos, bloqueados));
        }

        return secoes;
    }

    private bool EstaModuloAtivo(ModuloSistema modulo)
    {
        return (_modulosAtivos & modulo) == modulo;
    }

    private static Action _abrirFormularioCallback = () => { };

    public static void DefinirCallbackAbrir(Action<Type> callback)
    {
        _abrirFormularioCallback = () => { };
    }

    private void AbrirFormulario(Type tipoFormulario)
    {
        // Será implementado no FrmMain com injeção de dependência
        // Por enquanto, apenas placeholder
    }
}
