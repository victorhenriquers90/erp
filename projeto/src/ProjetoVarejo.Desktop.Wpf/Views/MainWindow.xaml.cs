using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

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

        // Re-renderiza o gráfico de barras quando o canvas é dimensionado/redimensionado.
        ChartBarras.SizeChanged += (_, _) => DesenharBarras();

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

    private readonly CultureInfo _ptBr = new("pt-BR");
    private (string mes, decimal pagar, decimal receber)[] _buckets = [];
    private decimal _totalPagar;
    private decimal _totalReceber;

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

            await CarregarGraficosAsync(db);
        }
        catch
        {
            LblKpiClientes.Text = "-";
            LblKpiProdutos.Text = "-";
            LblKpiFornecedores.Text = "-";
            LblKpiFinanceiro.Text = "-";
        }
    }

    // ───────────── Gráficos do dashboard ─────────────

    private async Task CarregarGraficosAsync(AppDbContext db)
    {
        var contas = await db.ContasFinanceiras.AsNoTracking()
            .Where(c => c.Ativo && c.Status == StatusConta.EmAberto)
            .Select(c => new { c.Tipo, c.Valor, c.DataVencimento })
            .ToListAsync();

        _totalPagar = contas.Where(c => c.Tipo == TipoConta.Pagar).Sum(c => c.Valor);
        _totalReceber = contas.Where(c => c.Tipo == TipoConta.Receber).Sum(c => c.Valor);

        var inicio = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var buckets = new List<(string, decimal, decimal)>();
        for (int m = 0; m < 6; m++)
        {
            var ini = inicio.AddMonths(m);
            var fim = ini.AddMonths(1);
            var pagar = contas.Where(c => c.Tipo == TipoConta.Pagar && c.DataVencimento >= ini && c.DataVencimento < fim).Sum(c => c.Valor);
            var receber = contas.Where(c => c.Tipo == TipoConta.Receber && c.DataVencimento >= ini && c.DataVencimento < fim).Sum(c => c.Valor);
            buckets.Add((ini.ToString("MMM/yy", _ptBr), pagar, receber));
        }
        _buckets = buckets.ToArray();

        LblDonutTotal.Text = (_totalPagar + _totalReceber).ToString("C0", _ptBr);
        LblDonutPagar.Text = _totalPagar.ToString("C0", _ptBr);
        LblDonutReceber.Text = _totalReceber.ToString("C0", _ptBr);

        DesenharBarras();
        DesenharDonut();
    }

    private void DesenharBarras()
    {
        var c = ChartBarras;
        c.Children.Clear();
        if (_buckets.Length == 0) return;
        double w = c.ActualWidth, h = c.ActualHeight;
        if (w < 10 || h < 10) return;

        double maxVal = (double)Math.Max(1m, _buckets.Max(b => Math.Max(b.pagar, b.receber)));
        double baseY = h - 20;                 // espaço p/ rótulo do mês
        double groupW = w / _buckets.Length;
        double barW = Math.Min(20, groupW * 0.28);

        for (int i = 0; i < _buckets.Length; i++)
        {
            double gx = i * groupW;
            double centro = gx + groupW / 2;
            AddBarra(c, centro - barW - 3, baseY, barW, (double)_buckets[i].pagar / maxVal * (baseY - 6),
                Color.FromRgb(0xF8, 0x71, 0x71), Color.FromRgb(0xEF, 0x44, 0x44));
            AddBarra(c, centro + 3, baseY, barW, (double)_buckets[i].receber / maxVal * (baseY - 6),
                Color.FromRgb(0x4A, 0xD9, 0xA8), Color.FromRgb(0x10, 0xB9, 0x81));

            var lbl = new TextBlock { Text = _buckets[i].mes, FontSize = 11, Foreground = Pincel("TextSoft") };
            lbl.Measure(new Size(groupW, 20));
            Canvas.SetLeft(lbl, centro - lbl.DesiredSize.Width / 2);
            Canvas.SetTop(lbl, h - 16);
            c.Children.Add(lbl);
        }
    }

    private static void AddBarra(Canvas c, double x, double baseY, double w, double height, Color topo, Color baixo)
    {
        if (height < 2) height = 2;
        var b = new Border
        {
            Width = w,
            Height = height,
            CornerRadius = new CornerRadius(5, 5, 0, 0),
            Background = new LinearGradientBrush(baixo, topo, new Point(0, 1), new Point(0, 0))
        };
        Canvas.SetLeft(b, x);
        Canvas.SetTop(b, baseY - height);
        c.Children.Add(b);
    }

    private void DesenharDonut()
    {
        var c = ChartDonut;
        c.Children.Clear();
        const double cx = 90, cy = 90, rOut = 84, rIn = 56;
        var total = _totalPagar + _totalReceber;

        if (total <= 0) { DesenharAnel(c, cx, cy, rOut, rIn, Color.FromRgb(0xE5, 0xE7, 0xEB)); return; }
        if (_totalPagar <= 0) { DesenharAnel(c, cx, cy, rOut, rIn, Color.FromRgb(0x10, 0xB9, 0x81)); return; }
        if (_totalReceber <= 0) { DesenharAnel(c, cx, cy, rOut, rIn, Color.FromRgb(0xEF, 0x44, 0x44)); return; }

        double sweepPagar = (double)(_totalPagar / total) * 360.0;
        c.Children.Add(BuildArc(cx, cy, rOut, rIn, 0, sweepPagar, new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))));
        c.Children.Add(BuildArc(cx, cy, rOut, rIn, sweepPagar, 360.0 - sweepPagar, new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81))));
    }

    private static void DesenharAnel(Canvas c, double cx, double cy, double rOut, double rIn, Color cor)
    {
        double rMid = (rOut + rIn) / 2;
        var e = new Ellipse
        {
            Width = rMid * 2,
            Height = rMid * 2,
            Stroke = new SolidColorBrush(cor),
            StrokeThickness = rOut - rIn,
            Fill = null
        };
        Canvas.SetLeft(e, cx - rMid);
        Canvas.SetTop(e, cy - rMid);
        c.Children.Add(e);
    }

    private static Path BuildArc(double cx, double cy, double rOut, double rIn, double startDeg, double sweepDeg, Brush fill)
    {
        var p0o = PontoCirculo(cx, cy, rOut, startDeg);
        var p1o = PontoCirculo(cx, cy, rOut, startDeg + sweepDeg);
        var p1i = PontoCirculo(cx, cy, rIn, startDeg + sweepDeg);
        var p0i = PontoCirculo(cx, cy, rIn, startDeg);
        bool grande = sweepDeg > 180;

        var fig = new PathFigure { StartPoint = p0o, IsClosed = true };
        fig.Segments.Add(new ArcSegment(p1o, new Size(rOut, rOut), 0, grande, SweepDirection.Clockwise, true));
        fig.Segments.Add(new LineSegment(p1i, true));
        fig.Segments.Add(new ArcSegment(p0i, new Size(rIn, rIn), 0, grande, SweepDirection.Counterclockwise, true));
        var geo = new PathGeometry();
        geo.Figures.Add(fig);
        return new Path { Data = geo, Fill = fill };
    }

    private static Point PontoCirculo(double cx, double cy, double r, double deg)
    {
        double rad = (deg - 90) * Math.PI / 180.0; // 0° = topo, sentido horário
        return new Point(cx + r * Math.Cos(rad), cy + r * Math.Sin(rad));
    }

    private Brush Pincel(string chave) => (Brush)FindResource(chave);

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
