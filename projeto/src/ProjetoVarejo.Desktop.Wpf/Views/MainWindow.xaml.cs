using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Infrastructure.Data;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class MainWindow : Window
{
    private readonly SessaoApp _sessao;
    private readonly IServiceProvider _services;

    // Escopo de DI do módulo atualmente exibido (mantém o DbContext vivo enquanto a tela está aberta).
    private IServiceScope? _moduloScope;

    public MainWindow(SessaoApp sessao, IServiceProvider services)
    {
        _sessao = sessao;
        _services = services;
        InitializeComponent();

        var nomeUsuario = _sessao.UsuarioLogado?.Nome ?? "Usuario";
        var nomeEmpresa = _sessao.EmpresaAtiva?.NomeFantasia ?? _sessao.EmpresaAtiva?.RazaoSocial ?? "Empresa";

        LblUsuario.Text = nomeUsuario;
        LblEmpresa.Text = nomeEmpresa;

        // Top bar
        LblTopUsuario.Text = nomeUsuario;
        LblTopEmpresa.Text = nomeEmpresa;
        LblAvatar.Text = string.IsNullOrWhiteSpace(nomeUsuario) ? "?" : nomeUsuario.Trim()[..1].ToUpperInvariant();

        // Saudação + data
        var hora = DateTime.Now.Hour;
        var saudacao = hora < 12 ? "Bom dia" : hora < 18 ? "Boa tarde" : "Boa noite";
        var primeiroNome = nomeUsuario.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nomeUsuario;
        LblSaudacao.Text = $"{saudacao}, {primeiroNome}!";
        LblData.Text = DateTime.Now.ToString("dddd, dd 'de' MMMM 'de' yyyy",
            new System.Globalization.CultureInfo("pt-BR"));

        // Realce do item ativo: um unico handler captura o clique de qualquer botao do menu.
        MenuPanel.AddHandler(Button.ClickEvent, new RoutedEventHandler(OnMenuButtonClick));
        MarcarMenuAtivo(BtnPainel);

        // Barra de status
        LblStatusUsuario.Text = nomeUsuario;
        LblStatusEmpresa.Text = nomeEmpresa;
        IniciarRelogio();

        _ = CarregarKpisAsync();
    }

    private void IniciarRelogio()
    {
        LblRelogio.Text = DateTime.Now.ToString("HH:mm");
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        timer.Tick += (_, _) => LblRelogio.Text = DateTime.Now.ToString("HH:mm");
        timer.Start();
    }

    // ───────────── Realce do item de menu ativo ─────────────
    private Button? _menuAtivo;

    private void OnMenuButtonClick(object sender, RoutedEventArgs e)
    {
        // Itens marcados como "nonav" (ex.: links para o sistema comercial) não navegam,
        // então não devem receber o realce de item ativo.
        if (e.Source is Button b && (b.Tag as string) != "nonav")
            MarcarMenuAtivo(b);
    }

    private void MarcarMenuAtivo(Button botao)
    {
        if (_menuAtivo != null) _menuAtivo.Tag = null;
        botao.Tag = "active";
        _menuAtivo = botao;
    }

    private async Task CarregarKpisAsync()
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var clientes = await db.Clientes.CountAsync(c => c.Ativo);
            var produtos = await db.Produtos.CountAsync(p => p.Ativo);
            var fornecedores = await db.Fornecedores.CountAsync(f => f.Ativo);
            var contasAbertas = await db.ContasFinanceiras.CountAsync(c => c.Ativo);

            LblKpiClientes.Text = clientes.ToString("N0");
            LblKpiProdutos.Text = produtos.ToString("N0");
            LblKpiFornecedores.Text = fornecedores.ToString("N0");
            LblKpiFinanceiro.Text = contasAbertas.ToString("N0");
        }
        catch
        {
            LblKpiClientes.Text = "-";
            LblKpiProdutos.Text = "-";
            LblKpiFornecedores.Text = "-";
            LblKpiFinanceiro.Text = "-";
        }
    }

    // ───────────── Navegação do shell ─────────────

    /// <summary>Exibe um módulo (UserControl) embutido na janela, trocando o conteúdo central.</summary>
    private void NavegarModulo<TView>(string titulo, string breadcrumb) where TView : UserControl
    {
        _moduloScope?.Dispose();
        _moduloScope = _services.CreateScope();

        var view = _moduloScope.ServiceProvider.GetRequiredService<TView>();
        ContentHost.Content = view;
        ContentHost.Visibility = Visibility.Visible;
        DashboardRoot.Visibility = Visibility.Collapsed;

        LblPagina.Text = titulo;
        LblBreadcrumb.Text = "Início · " + breadcrumb;
    }

    /// <summary>Volta para o painel (dashboard).</summary>
    private void MostrarDashboard()
    {
        ContentHost.Content = null;
        ContentHost.Visibility = Visibility.Collapsed;
        DashboardRoot.Visibility = Visibility.Visible;
        _moduloScope?.Dispose();
        _moduloScope = null;

        LblPagina.Text = "Painel";
        LblBreadcrumb.Text = "Início · Visão geral";
        _ = CarregarKpisAsync();
    }

    private void Painel_Click(object sender, RoutedEventArgs e) => MostrarDashboard();

    private void Clientes_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<ClientesWindow>("Clientes", "Cadastros · Clientes");

    private void Filiais_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<FiliaisWindow>("Filiais / Empresas", "Cadastros · Filiais");

    private void Configuracoes_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<ConfiguracoesWindow>("Configurações", "Administração · Configurações");

    private void Produtos_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<ProdutosWindow>("Produtos", "Cadastros · Produtos");

    private void Fornecedores_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<FornecedoresWindow>("Fornecedores", "Cadastros · Fornecedores");

    private void Aquisicao_Click(object sender, RoutedEventArgs e) => Fornecedores_Click(sender, e);

    private void CadeiaSuprimentos_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<CadeiaSuprimentosWindow>("Cadeia de Suprimentos", "Operações · Suprimentos");

    private void Estoque_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<EstoqueWindow>("Estoque", "Operações · Estoque");

    private void Financeiro_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<FinanceiroWindow>("Financeiro", "Financeiro · Contas");

    private void Faturamento_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<FaturamentoWindow>("Faturamento (NF-e)", "Financeiro · Faturamento");

    private void Armazem_Click(object sender, RoutedEventArgs e) => Estoque_Click(sender, e);

    private void Pedidos_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<PedidosWindow>("Pedidos", "Operações · Pedidos");

    private void Producao_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<ProducaoWindow>("Produção", "Manufatura · Produção");

    private void Projetos_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<ProjetosWindow>("Projetos", "Manufatura · Projetos");

    private void ForcaTrabalho_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<ForcaTrabalhoWindow>("Força de Trabalho", "Pessoas · Força de Trabalho");

    private void Horas_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<ApontamentoHorasWindow>("Apontamento de Horas", "Pessoas · Horas");

    private void Relatorios_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<RelatoriosWindow>("Relatórios", "Financeiro · Relatórios");

    private void Auditoria_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<AuditoriaWindow>("Auditoria", "Administração · Auditoria");

    private void Ecommerce_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<EcommerceWindow>("E-Commerce", "Vendas · E-Commerce");

    private void Marketing_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<MarketingWindow>("Marketing", "Vendas · Marketing");

    private void Fiscal_Click(object sender, RoutedEventArgs e)
        => NavegarModulo<FiscalWindow>("Fiscal / NFC-e", "Administração · Fiscal");

    private void ComercialExterno_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "O PDV e o Caixa Comercial pertencem ao Sistema Gestão (módulo comercial separado).",
            "Projeto ERP",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Indisponivel_Click(object sender, RoutedEventArgs e)
        => AbrirModuloPlanejado("Este módulo");

    private void AbrirModuloPlanejado(string nomeModulo)
    {
        MessageBox.Show(
            $"{nomeModulo} ja esta previsto na arquitetura do ERP e sera liberado na proxima etapa de implantacao.",
            "Modulo em implantacao",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void Sair_Click(object sender, RoutedEventArgs e) => Close();
}
