using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class FiliaisWindow : UserControl
{
    private readonly NfceService _nfceService;
    private readonly FilialPainelService _painelService;
    private readonly EmpresaEditorWindow _empresaEditorWindow;
    private readonly CultureInfo _ptBr = new("pt-BR");
    private List<EmpresaConfig> _empresas = [];

    public FiliaisWindow(NfceService nfceService, FilialPainelService painelService,
                         EmpresaEditorWindow empresaEditorWindow)
    {
        _nfceService = nfceService;
        _painelService = painelService;
        _empresaEditorWindow = empresaEditorWindow;
        InitializeComponent();
        Loaded += async (_, _) => await CarregarAsync();
    }

    // ─── Aba Cadastro ────────────────────────────────────────────────────────

    private async Task CarregarAsync()
    {
        _empresas = await _nfceService.ListarEmpresasAsync();
        var filtro = TxtBusca.Text.Trim();
        var lista = string.IsNullOrWhiteSpace(filtro)
            ? _empresas
            : _empresas.Where(e =>
                (e.RazaoSocial?.Contains(filtro, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.NomeFantasia?.Contains(filtro, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Cnpj?.Contains(filtro, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

        DgEmpresas.ItemsSource = lista.Select(e => new EmpresaLinhaUi(
            e.Id,
            e.Apelido ?? "",
            e.RazaoSocial,
            string.IsNullOrWhiteSpace(e.NomeFantasia) ? "-" : e.NomeFantasia,
            string.IsNullOrWhiteSpace(e.Cnpj) ? "-" : e.Cnpj,
            string.Join("/", new[] { e.Cidade, e.Uf }.Where(v => !string.IsNullOrWhiteSpace(v))),
            e.UrlApi ?? "(local)",
            e.AmbienteHomologacao ? "Homologação" : "Produção")).ToList();

        LblResumo.Text = $"{lista.Count} empresa(s) / filial(is) cadastrada(s)";
    }

    private EmpresaConfig? ObterSelecionada()
    {
        if (DgEmpresas.SelectedItem is not EmpresaLinhaUi row) return null;
        return _empresas.FirstOrDefault(e => e.Id == row.Id);
    }

    private async void TxtBusca_TextChanged(object sender, TextChangedEventArgs e) => await CarregarAsync();
    private async void Atualizar_Click(object sender, RoutedEventArgs e) => await CarregarAsync();

    private async void Novo_Click(object sender, RoutedEventArgs e)
    {
        var nova = new EmpresaConfig { Ativo = true, AmbienteHomologacao = true };
        if (_empresaEditorWindow.Abrir(Window.GetWindow(this)!, nova))
            await CarregarAsync();
    }

    private async void Editar_Click(object sender, RoutedEventArgs e)
    {
        var sel = ObterSelecionada();
        if (sel == null)
        {
            MessageBox.Show("Selecione uma empresa para editar.", "Filiais",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var empresa = await _nfceService.ObterEmpresaPorIdAsync(sel.Id) ?? sel;
        if (_empresaEditorWindow.Abrir(Window.GetWindow(this)!, empresa))
            await CarregarAsync();
    }

    // ─── Aba Painel de Rede ──────────────────────────────────────────────────

    private void TabPrincipal_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private async void AtualizarPainel_Click(object sender, RoutedEventArgs e)
    {
        BtnAtualizarPainel.IsEnabled = false;
        var btnContent = new StackPanel { Orientation = Orientation.Horizontal };
        btnContent.Children.Add(new TextBlock { Text = "Consultando...", VerticalAlignment = VerticalAlignment.Center });
        BtnAtualizarPainel.Content = btnContent;
        LblUltimaConsulta.Text = "Consultando filiais — pode levar alguns segundos...";
        PainelFiliais.Children.Clear();
        PainelKpisConsolidados.Visibility = Visibility.Collapsed;

        try
        {
            var status = await _painelService.ObterStatusTodasAsync();
            RenderizarPainel(status);
            LblUltimaConsulta.Text = $"Atualizado em {DateTime.Now:HH:mm:ss}  •  {status.Count} filial(is) consultada(s)";
        }
        catch (Exception ex)
        {
            LblUltimaConsulta.Text = $"Erro ao consultar: {ex.Message}";
        }
        finally
        {
            BtnAtualizarPainel.IsEnabled = true;
            BtnAtualizarPainel.Content = "↻  Atualizar status";
        }
    }

    private void RenderizarPainel(List<FilialStatus> filiais)
    {
        var online = filiais.Count(f => f.Online);
        LblOnline.Text = $"{online}/{filiais.Count}";
        LblOnline.Foreground = online == filiais.Count
            ? new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80))
            : new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
        LblVendasTotal.Text = filiais.Sum(f => f.VendasHoje).ToString("C", _ptBr);
        LblCaixasAbertos.Text = filiais.Count(f => f.CaixaAberto).ToString();
        LblContasAtrasadas.Text = filiais.Sum(f => f.ContasAtrasadas).ToString();
        PainelKpisConsolidados.Visibility = Visibility.Visible;

        foreach (var f in filiais)
            PainelFiliais.Children.Add(CriarCardFilial(f));
    }

    private Border CriarCardFilial(FilialStatus f)
    {
        var corBorda = f.Online
            ? Color.FromRgb(0x2A, 0x3E, 0x55)
            : Color.FromRgb(0xF8, 0x71, 0x71);

        var card = new Border
        {
            Width = 260, Margin = new Thickness(0, 0, 16, 16),
            CornerRadius = new CornerRadius(12),
            Background = (Brush)FindResource("BgCard"),
            BorderBrush = new SolidColorBrush(corBorda),
            BorderThickness = new Thickness(1.5),
            Padding = new Thickness(18)
        };

        var painel = new StackPanel();

        // Cabeçalho: nome + bolinha de status
        var cab = new Grid();
        cab.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        cab.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lblNome = new TextBlock
        {
            Text = f.Apelido ?? f.Nome,
            FontSize = 15, FontWeight = FontWeights.Bold,
            Foreground = (Brush)FindResource("TextStrong"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        var dot = new Border
        {
            Width = 10, Height = 10, CornerRadius = new CornerRadius(5),
            Background = new SolidColorBrush(f.Online
                ? Color.FromRgb(0x4A, 0xDE, 0x80) : Color.FromRgb(0xF8, 0x71, 0x71)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(dot, 1);
        cab.Children.Add(lblNome);
        cab.Children.Add(dot);
        painel.Children.Add(cab);

        painel.Children.Add(new TextBlock
        {
            Text = f.Online
                ? (string.IsNullOrEmpty(f.UrlApi) ? "• Local" : $"• {f.UrlApi}")
                : "• OFFLINE",
            FontSize = 11, Foreground = (Brush)FindResource("TextSoft"),
            Margin = new Thickness(0, 2, 0, 12)
        });

        if (f.Online)
        {
            painel.Children.Add(KpiLinha("💰 Vendas hoje", f.VendasHoje.ToString("C", _ptBr)));
            painel.Children.Add(KpiLinha("🛒 Pedidos", f.PedidosHoje.ToString()));
            painel.Children.Add(KpiLinha("🏧 Caixa",
                f.CaixaAberto ? "Aberto" : "Fechado",
                f.CaixaAberto ? Color.FromRgb(0x4A, 0xDE, 0x80) : Color.FromRgb(0xFB, 0xBF, 0x24)));
            painel.Children.Add(KpiLinha("📊 Saldo previsto",
                f.SaldoPrevisto.ToString("C", _ptBr),
                f.SaldoPrevisto >= 0 ? Color.FromRgb(0x4A, 0xDE, 0x80) : Color.FromRgb(0xF8, 0x71, 0x71)));
            if (f.ContasAtrasadas > 0)
                painel.Children.Add(KpiLinha("⚠️ Atrasadas",
                    f.ContasAtrasadas.ToString(), Color.FromRgb(0xF8, 0x71, 0x71)));

            painel.Children.Add(new TextBlock
            {
                Text = $"v{f.Versao}  •  {f.ConsultadaEm:HH:mm:ss}",
                FontSize = 10, Foreground = (Brush)FindResource("TextSoft"),
                Margin = new Thickness(0, 10, 0, 0)
            });
        }
        else
        {
            painel.Children.Add(new TextBlock
            {
                Text = "Sem conexão. Verifique a rede ou a URL da API cadastrada.",
                FontSize = 12, TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71)),
                Margin = new Thickness(0, 4, 0, 0)
            });
        }

        card.Child = painel;
        return card;
    }

    private static StackPanel KpiLinha(string label, string valor, Color? cor = null)
    {
        var s = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
        s.Children.Add(new TextBlock
        {
            Text = label, FontSize = 12, Width = 145,
            Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0xA3, 0xBE))
        });
        s.Children.Add(new TextBlock
        {
            Text = valor, FontSize = 12, FontWeight = FontWeights.SemiBold,
            Foreground = cor.HasValue
                ? new SolidColorBrush(cor.Value)
                : new SolidColorBrush(Color.FromRgb(0xE5, 0xED, 0xF5))
        });
        return s;
    }
}

public sealed record EmpresaLinhaUi(
    int Id, string Apelido, string RazaoSocial, string NomeFantasia,
    string Cnpj, string CidadeUf, string UrlApi, string Ambiente);
