using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Infrastructure.Reporting;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class RelatoriosWindow : UserControl
{
    private readonly RelatorioService _relatorioService;
    private readonly RelatorioExporter _exporter;
    private readonly IServiceProvider _serviceProvider;
    private readonly CultureInfo _ptBr = new("pt-BR");

    private List<object> _diaRows = [];
    private List<object> _formaRows = [];
    private List<object> _vendedorRows = [];
    private List<object> _abcRows = [];
    private List<object> _topRows = [];
    private List<object> _fluxoRows = [];

    public RelatoriosWindow(RelatorioService relatorioService, RelatorioExporter exporter, IServiceProvider serviceProvider)
    {
        _relatorioService = relatorioService;
        _exporter = exporter;
        _serviceProvider = serviceProvider;
        InitializeComponent();
        DtDe.SelectedDate = DateTime.Today.AddDays(-30);
        DtAte.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var de = DtDe.SelectedDate ?? DateTime.Today.AddDays(-30);
        var ate = (DtAte.SelectedDate ?? DateTime.Today).AddDays(1);

        var dia = await _relatorioService.VendasPorDiaAsync(de, ate);
        _diaRows = dia.Select(x => (object)new
        {
            Dia = x.Dia.ToString("dd/MM/yyyy"),
            Quantidade = x.Quantidade.ToString("N0", _ptBr),
            Total = x.Total.ToString("C", _ptBr)
        }).ToList();
        DgDia.ItemsSource = _diaRows;

        var forma = await _relatorioService.VendasPorFormaPagamentoAsync(de, ate);
        _formaRows = forma.Select(x => (object)new
        {
            Forma = x.Forma.ToString(),
            Qtd = x.Qtd.ToString("N0", _ptBr),
            Total = x.Total.ToString("C", _ptBr)
        }).ToList();
        DgForma.ItemsSource = _formaRows;

        var vendedor = await _relatorioService.VendasPorVendedorAsync(de, ate);
        _vendedorRows = vendedor.Select(x => (object)new
        {
            Vendedor = x.Vendedor,
            Qtd = x.QtdVendas.ToString("N0", _ptBr),
            Total = x.Total.ToString("C", _ptBr),
            Ticket = x.TicketMedio.ToString("C", _ptBr)
        }).ToList();
        DgVendedor.ItemsSource = _vendedorRows;

        var abc = await _relatorioService.CurvaAbcAsync(de, ate);
        _abcRows = abc.Select(x => (object)new
        {
            x.Codigo,
            x.Descricao,
            Quantidade = x.Quantidade.ToString("N3", _ptBr),
            Faturamento = x.Faturamento.ToString("C", _ptBr),
            x.Classe
        }).ToList();
        DgAbc.ItemsSource = _abcRows;

        var top = await _relatorioService.TopProdutosAsync(de, ate, 50);
        _topRows = top.Select(x => (object)new
        {
            x.Codigo,
            x.Descricao,
            Quantidade = x.Quantidade.ToString("N3", _ptBr),
            Faturamento = x.Faturamento.ToString("C", _ptBr)
        }).ToList();
        DgTop.ItemsSource = _topRows;

        var fluxo = await _relatorioService.FluxoCaixaAsync(de, ate);
        _fluxoRows = fluxo.Select(x => (object)new
        {
            Dia = x.Dia.ToString("dd/MM/yyyy"),
            Entradas = x.Entradas.ToString("C", _ptBr),
            Saidas = x.Saidas.ToString("C", _ptBr),
            Saldo = x.Saldo.ToString("C", _ptBr)
        }).ToList();
        DgFluxo.ItemsSource = _fluxoRows;

        var totalVendido = dia.Sum(x => x.Total);
        LblResumo.Text = $"Periodo {de:dd/MM/yyyy} a {ate.AddDays(-1):dd/MM/yyyy}  •  Total vendido: {totalVendido.ToString("C", _ptBr)}";
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }

    private void Exportar_Click(object sender, RoutedEventArgs e)
    {
        if (!TentarObterAbaAtual(out var tab, out var grid))
        {
            MessageBox.Show("Nada para exportar.", "Relatorios", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sfd = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"{NormalizarNomeArquivo(tab.Header?.ToString())}_{DateTime.Now:yyyyMMdd}.csv"
        };
        if (sfd.ShowDialog(Window.GetWindow(this)) != true) return;

        File.WriteAllText(sfd.FileName, GerarCsv(grid), new UTF8Encoding(true));
        MessageBox.Show($"Exportado: {Path.GetFileName(sfd.FileName)}", "Relatorios", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportarExcel_Click(object sender, RoutedEventArgs e)
    {
        var de = DtDe.SelectedDate ?? DateTime.Today.AddDays(-30);
        var ate = DtAte.SelectedDate ?? DateTime.Today;
        var (bytes, nome) = GerarExcel(de, ate);
        if (bytes == null) { MessageBox.Show("Nada para exportar.", "Relatorios", MessageBoxButton.OK, MessageBoxImage.Information); return; }

        var sfd = new SaveFileDialog
        {
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"{nome}_{DateTime.Now:yyyyMMdd}.xlsx"
        };
        if (sfd.ShowDialog(Window.GetWindow(this)) != true) return;
        File.WriteAllBytes(sfd.FileName, bytes);
        MessageBox.Show($"Exportado: {Path.GetFileName(sfd.FileName)}", "Relatorios", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportarPdf_Click(object sender, RoutedEventArgs e)
    {
        var de = DtDe.SelectedDate ?? DateTime.Today.AddDays(-30);
        var ate = DtAte.SelectedDate ?? DateTime.Today;
        var (bytes, nome) = GerarPdf(de, ate);
        if (bytes == null) { MessageBox.Show("Nada para exportar.", "Relatorios", MessageBoxButton.OK, MessageBoxImage.Information); return; }

        var sfd = new SaveFileDialog
        {
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"{nome}_{DateTime.Now:yyyyMMdd}.pdf"
        };
        if (sfd.ShowDialog(Window.GetWindow(this)) != true) return;
        File.WriteAllBytes(sfd.FileName, bytes);
        MessageBox.Show($"Exportado: {Path.GetFileName(sfd.FileName)}", "Relatorios", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private (byte[]? bytes, string nome) GerarExcel(DateTime de, DateTime ate)
    {
        var aba = (TabsRelatorios.SelectedItem as TabItem)?.Header?.ToString() ?? "";
        return aba switch
        {
            "Vendas por dia" => (_exporter.GerarVendasPorDiaExcel(
                _diaRows.Select(r => { var t = r.GetType(); return new VendaDiariaDto(DateTime.Parse(t.GetProperty("Dia")!.GetValue(r)!.ToString()!), int.Parse(t.GetProperty("Quantidade")!.GetValue(r)!.ToString()!.Replace(".", "")), decimal.Parse(t.GetProperty("Total")!.GetValue(r)!.ToString()!.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim())); }).ToList(), de, ate), "Vendas_por_Dia"),
            "Curva ABC" => (_exporter.GerarCurvaAbcExcel(
                _abcRows.Select(r => { var t = r.GetType(); return new ProdutoRankingDto(t.GetProperty("Codigo")!.GetValue(r)?.ToString() ?? "", t.GetProperty("Descricao")!.GetValue(r)?.ToString() ?? "", decimal.Parse(t.GetProperty("Qtd")!.GetValue(r)!.ToString()!.Replace(".", "").Replace(",", ".").Trim()), decimal.Parse(t.GetProperty("Faturamento")!.GetValue(r)!.ToString()!.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim()), t.GetProperty("Classe")!.GetValue(r)?.ToString() ?? ""); }).ToList(), de, ate), "Curva_ABC"),
            "Fluxo de caixa" => (_exporter.GerarFluxoCaixaExcel(
                _fluxoRows.Select(r => { var t = r.GetType(); return new FluxoCaixaDto(DateTime.Parse(t.GetProperty("Dia")!.GetValue(r)!.ToString()!), decimal.Parse(t.GetProperty("Entradas")!.GetValue(r)!.ToString()!.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim()), decimal.Parse(t.GetProperty("Saidas")!.GetValue(r)!.ToString()!.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim()), decimal.Parse(t.GetProperty("Saldo")!.GetValue(r)!.ToString()!.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim())); }).ToList(), de, ate), "Fluxo_de_Caixa"),
            _ => (null, "")
        };
    }

    private (byte[]? bytes, string nome) GerarPdf(DateTime de, DateTime ate)
    {
        var aba = (TabsRelatorios.SelectedItem as TabItem)?.Header?.ToString() ?? "";
        return aba switch
        {
            "Vendas por dia" => (_exporter.GerarVendasPorDiaPdf(
                _diaRows.Select(r => { var t = r.GetType(); return new VendaDiariaDto(DateTime.Parse(t.GetProperty("Dia")!.GetValue(r)!.ToString()!), int.Parse(t.GetProperty("Quantidade")!.GetValue(r)!.ToString()!.Replace(".", "")), decimal.Parse(t.GetProperty("Total")!.GetValue(r)!.ToString()!.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim())); }).ToList(), de, ate), "Vendas_por_Dia"),
            "Curva ABC" => (_exporter.GerarCurvaAbcPdf(
                _abcRows.Select(r => { var t = r.GetType(); return new ProdutoRankingDto(t.GetProperty("Codigo")!.GetValue(r)?.ToString() ?? "", t.GetProperty("Descricao")!.GetValue(r)?.ToString() ?? "", decimal.Parse(t.GetProperty("Qtd")!.GetValue(r)!.ToString()!.Replace(".", "").Replace(",", ".").Trim()), decimal.Parse(t.GetProperty("Faturamento")!.GetValue(r)!.ToString()!.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim()), t.GetProperty("Classe")!.GetValue(r)?.ToString() ?? ""); }).ToList(), de, ate), "Curva_ABC"),
            "Fluxo de caixa" => (_exporter.GerarFluxoCaixaPdf(
                _fluxoRows.Select(r => { var t = r.GetType(); return new FluxoCaixaDto(DateTime.Parse(t.GetProperty("Dia")!.GetValue(r)!.ToString()!), decimal.Parse(t.GetProperty("Entradas")!.GetValue(r)!.ToString()!.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim()), decimal.Parse(t.GetProperty("Saidas")!.GetValue(r)!.ToString()!.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim()), decimal.Parse(t.GetProperty("Saldo")!.GetValue(r)!.ToString()!.Replace("R$", "").Replace(".", "").Replace(",", ".").Trim())); }).ToList(), de, ate), "Fluxo_de_Caixa"),
            _ => (null, "")
        };
    }

    private void DetalhesTecnicos_Click(object sender, RoutedEventArgs e)
    {
        if (!TentarObterAbaAtual(out var tab, out var grid))
        {
            MessageBox.Show("Nenhuma aba com dados para detalhar.", "Relatorios", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var csv = GerarCsv(grid);
        var linhas = grid.Items.Cast<object>().Count(i => i != CollectionView.NewItemPlaceholder);
        var abaAtiva = tab.Header?.ToString() ?? "Aba atual";
        var de = DtDe.SelectedDate ?? DateTime.Today.AddDays(-30);
        var ate = DtAte.SelectedDate ?? DateTime.Today;
        var periodo = $"{de:dd/MM/yyyy} a {ate:dd/MM/yyyy}";
        var resumo = $"{LblResumo.Text}{Environment.NewLine}Aba: {abaAtiva}{Environment.NewLine}Registros: {linhas:N0}";

        var janela = _serviceProvider.GetRequiredService<RelatorioDetalhesTecnicosWindow>();
        janela.Owner = Window.GetWindow(this);
        janela.Carregar(abaAtiva, periodo, resumo, csv);
        janela.ShowDialog();
    }

    private static string Escape(string s)
    {
        if (s.Contains(';') || s.Contains('"') || s.Contains('\n'))
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }

    private static string GerarCsv(DataGrid grid)
    {
        var sb = new StringBuilder();
        var cols = grid.Columns.ToList();
        sb.AppendLine(string.Join(";", cols.Select(c => Escape(c.Header?.ToString() ?? ""))));

        foreach (var item in grid.Items)
        {
            if (item == CollectionView.NewItemPlaceholder) continue;

            var line = new List<string>();
            foreach (var col in cols)
            {
                if (col is DataGridBoundColumn bound && bound.Binding is Binding b && b.Path != null)
                {
                    var prop = item.GetType().GetProperty(b.Path.Path);
                    var value = prop?.GetValue(item)?.ToString() ?? "";
                    line.Add(Escape(value));
                }
                else
                {
                    line.Add("");
                }
            }

            sb.AppendLine(string.Join(";", line));
        }

        return sb.ToString();
    }

    private bool TentarObterAbaAtual(out TabItem tab, out DataGrid grid)
    {
        tab = TabsRelatorios.SelectedItem as TabItem ?? new TabItem();
        grid = tab.Content as DataGrid ?? new DataGrid();
        return tab.Header != null && grid.Items.Count > 0;
    }

    private static string NormalizarNomeArquivo(string? nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return "Relatorio";

        var baseName = nome.Trim().Replace(" ", "_");
        foreach (var invalid in Path.GetInvalidFileNameChars())
            baseName = baseName.Replace(invalid, '_');
        return baseName;
    }
}
