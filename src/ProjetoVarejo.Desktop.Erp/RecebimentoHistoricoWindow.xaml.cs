using ClosedXML.Excel;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace ProjetoVarejo.Desktop.Erp;

public partial class RecebimentoHistoricoWindow : Window
{
    private readonly List<RecebimentoHistoricoLinhaUi> _originais = new();
    private readonly ObservableCollection<RecebimentoHistoricoLinhaUi> _linhas = new();
    private readonly ObservableCollection<RankingResumoLinhaUi> _rankingProdutos = new();
    private readonly ObservableCollection<RankingResumoLinhaUi> _rankingUsuarios = new();

    public RecebimentoHistoricoWindow(string tituloResumo, IEnumerable<RecebimentoHistoricoLinhaUi> linhas)
    {
        InitializeComponent();
        TxtResumo.Text = tituloResumo;
        DgHistorico.ItemsSource = _linhas;
        DgRankingProdutos.ItemsSource = _rankingProdutos;
        DgRankingUsuarios.ItemsSource = _rankingUsuarios;

        foreach (var linha in linhas)
        {
            _originais.Add(linha);
            _linhas.Add(linha);
        }

        if (_originais.Count > 0)
        {
            DpDe.SelectedDate = _originais.Min(x => x.DataHoraValor).Date;
            DpAte.SelectedDate = _originais.Max(x => x.DataHoraValor).Date;
        }

        AtualizarResumo();
    }

    private void BtnFiltrar_Click(object sender, RoutedEventArgs e)
    {
        var de = DpDe.SelectedDate?.Date;
        var ate = DpAte.SelectedDate?.Date;
        var filtroUsuario = TxtFiltroUsuario.Text?.Trim();
        var filtroProduto = TxtFiltroProduto.Text?.Trim();
        var somenteDivergencias = ChkSomenteDivergencias.IsChecked == true;
        var ordenarPorImpacto = ChkOrdenarImpacto.IsChecked == true;

        if (de.HasValue && ate.HasValue && de.Value > ate.Value)
        {
            MessageBox.Show("Periodo invalido: data inicial maior que data final.", "Historico", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var query = _originais.AsEnumerable();
        if (de.HasValue)
        {
            query = query.Where(x => x.DataHoraValor >= de.Value);
        }
        if (ate.HasValue)
        {
            var limiteFinal = ate.Value.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.DataHoraValor <= limiteFinal);
        }

        if (!string.IsNullOrWhiteSpace(filtroUsuario))
        {
            query = query.Where(x =>
                x.UsuarioValor.Contains(filtroUsuario, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtroProduto))
        {
            query = query.Where(x =>
                x.ProdutoValor.Contains(filtroProduto, StringComparison.OrdinalIgnoreCase));
        }

        if (somenteDivergencias)
        {
            query = query.Where(x => x.DivergenciaValor);
        }

        var ordenado = ordenarPorImpacto
            ? query.OrderByDescending(x => x.ImpactoDivergenciaNumero).ThenByDescending(x => x.DataHoraValor)
            : query.OrderByDescending(x => x.DataHoraValor);

        RecarregarLinhas(ordenado);
    }

    private void BtnLimpar_Click(object sender, RoutedEventArgs e)
    {
        DpDe.SelectedDate = null;
        DpAte.SelectedDate = null;
        TxtFiltroUsuario.Text = string.Empty;
        TxtFiltroProduto.Text = string.Empty;
        ChkSomenteDivergencias.IsChecked = false;
        ChkOrdenarImpacto.IsChecked = false;
        RecarregarLinhas(_originais.OrderByDescending(x => x.DataHoraValor));
    }

    private void RecarregarLinhas(IEnumerable<RecebimentoHistoricoLinhaUi> linhas)
    {
        _linhas.Clear();
        foreach (var linha in linhas)
        {
            _linhas.Add(linha);
        }

        AtualizarResumo();
    }

    private void AtualizarResumo()
    {
        var cultura = new CultureInfo("pt-BR");
        var totalEntradas = _linhas.Count;
        var totalItens = _linhas.Sum(x => x.QuantidadeValor);
        var totalDivergencias = _linhas.Count(x => x.DivergenciaValor);
        var totalImpacto = _linhas.Sum(x => x.ImpactoDivergenciaNumero);
        var impactoMedio = totalDivergencias > 0 ? totalImpacto / totalDivergencias : 0m;
        var totalPlanejado = _linhas.Sum(x => (x.ValorPlanejadoNumero ?? 0m) * x.QuantidadeValor);
        var totalRecebido = _linhas.Sum(x => (x.ValorRecebidoNumero ?? 0m) * x.QuantidadeValor);
        var variacao = totalRecebido - totalPlanejado;
        var variacaoPct = totalPlanejado > 0m ? (variacao / totalPlanejado) * 100m : 0m;

        TxtResumoEntradas.Text = totalEntradas.ToString("N0", cultura);
        TxtResumoItens.Text = totalItens.ToString("N3", cultura);
        TxtResumoDivergencias.Text = totalDivergencias.ToString("N0", cultura);
        TxtResumoImpacto.Text = totalImpacto.ToString("C2", cultura);
        TxtResumoImpactoMedio.Text = impactoMedio.ToString("C2", cultura);
        TxtResumoPlanejado.Text = totalPlanejado.ToString("C2", cultura);
        TxtResumoRecebido.Text = totalRecebido.ToString("C2", cultura);
        TxtResumoVariacao.Text = $"{variacao.ToString("C2", cultura)} ({variacaoPct.ToString("N2", cultura)}%)";

        AtualizarRankings(cultura);
    }

    private void AtualizarRankings(CultureInfo cultura)
    {
        _rankingProdutos.Clear();
        _rankingUsuarios.Clear();

        var produtos = _linhas
            .GroupBy(x => x.ProdutoValor)
            .Select(g => new
            {
                Nome = g.Key,
                Entradas = g.Count(),
                Quantidade = g.Sum(x => x.QuantidadeValor),
                Impacto = g.Sum(x => x.ImpactoDivergenciaNumero)
            })
            .OrderByDescending(x => x.Impacto)
            .ThenByDescending(x => x.Entradas)
            .Take(5)
            .ToList();

        foreach (var p in produtos)
        {
            _rankingProdutos.Add(new RankingResumoLinhaUi(
                p.Nome,
                p.Entradas.ToString("N0", cultura),
                p.Quantidade.ToString("N3", cultura),
                p.Impacto.ToString("C2", cultura)));
        }

        var usuarios = _linhas
            .GroupBy(x => x.UsuarioValor)
            .Select(g => new
            {
                Nome = g.Key,
                Entradas = g.Count(),
                Quantidade = g.Sum(x => x.QuantidadeValor),
                Impacto = g.Sum(x => x.ImpactoDivergenciaNumero)
            })
            .OrderByDescending(x => x.Impacto)
            .ThenByDescending(x => x.Entradas)
            .Take(5)
            .ToList();

        foreach (var u in usuarios)
        {
            _rankingUsuarios.Add(new RankingResumoLinhaUi(
                u.Nome,
                u.Entradas.ToString("N0", cultura),
                u.Quantidade.ToString("N3", cultura),
                u.Impacto.ToString("C2", cultura)));
        }
    }

    private void BtnExportarExcel_Click(object sender, RoutedEventArgs e)
    {
        if (_linhas.Count == 0)
        {
            MessageBox.Show("Nao ha dados para exportar.", "Historico", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var save = new SaveFileDialog
        {
            Title = "Salvar historico em Excel",
            Filter = "Excel (*.xlsx)|*.xlsx",
            FileName = $"historico-recebimento-{DateTime.Now:yyyyMMdd-HHmm}.xlsx"
        };

        if (save.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Historico");

            var headers = new[]
            {
                "Data/Hora", "Usuario", "Produto", "Qtd", "Valor Planejado", "Valor Recebido", "Divergencia", "Impacto", "Observacao"
            };

            for (var i = 0; i < headers.Length; i++)
            {
                ws.Cell(1, i + 1).Value = headers[i];
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }

            for (var row = 0; row < _linhas.Count; row++)
            {
                var item = _linhas[row];
                var r = row + 2;
                ws.Cell(r, 1).Value = item.DataHora;
                ws.Cell(r, 2).Value = item.Usuario;
                ws.Cell(r, 3).Value = item.Produto;
                ws.Cell(r, 4).Value = item.Quantidade;
                ws.Cell(r, 5).Value = item.ValorPlanejado;
                ws.Cell(r, 6).Value = item.ValorRecebido;
                ws.Cell(r, 7).Value = item.Divergencia;
                ws.Cell(r, 8).Value = item.ImpactoDivergencia;
                ws.Cell(r, 9).Value = item.Observacao;
            }

            ws.Columns().AdjustToContents();
            workbook.SaveAs(save.FileName);

            MessageBox.Show("Excel exportado com sucesso.", "Historico", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao exportar Excel: {ex.Message}", "Historico", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnExportarPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_linhas.Count == 0)
        {
            MessageBox.Show("Nao ha dados para exportar.", "Historico", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var save = new SaveFileDialog
        {
            Title = "Salvar historico em PDF",
            Filter = "PDF (*.pdf)|*.pdf",
            FileName = $"historico-recebimento-{DateTime.Now:yyyyMMdd-HHmm}.pdf"
        };

        if (save.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var resumo = TxtResumo.Text;
            var linhas = _linhas.ToList();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(18);
                    page.DefaultTextStyle(t => t.FontSize(9));
                    page.Header().Text(resumo).SemiBold().FontSize(14);

                    page.Content().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.8f);
                            columns.RelativeColumn(0.6f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(1.6f);
                        });

                        void AddHeader(string text)
                        {
                            table.Cell().Element(HeaderCell).Text(text).SemiBold();
                        }

                        AddHeader("Data/Hora");
                        AddHeader("Usuario");
                        AddHeader("Produto");
                        AddHeader("Qtd");
                        AddHeader("Planejado");
                        AddHeader("Recebido");
                        AddHeader("Diverg.");
                        AddHeader("Impacto");
                        AddHeader("Observacao");

                        foreach (var linha in linhas)
                        {
                            AddRow(linha.DataHora);
                            AddRow(linha.Usuario);
                            AddRow(linha.Produto);
                            AddRow(linha.Quantidade);
                            AddRow(linha.ValorPlanejado);
                            AddRow(linha.ValorRecebido);
                            AddRow(linha.Divergencia);
                            AddRow(linha.ImpactoDivergencia);
                            AddRow(linha.Observacao);
                        }

                        void AddRow(string text)
                        {
                            table.Cell().Element(BodyCell).Text(text);
                        }
                    });

                    page.Footer().AlignRight().Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}");
                });
            }).GeneratePdf(save.FileName);

            MessageBox.Show("PDF exportado com sucesso.", "Historico", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao exportar PDF: {ex.Message}", "Historico", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static IContainer HeaderCell(IContainer container)
    {
        return container
            .Background(Colors.Grey.Lighten3)
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .PaddingVertical(4)
            .PaddingHorizontal(3);
    }

    private static IContainer BodyCell(IContainer container)
    {
        return container
            .BorderBottom(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3)
            .PaddingHorizontal(3);
    }
}

public sealed record RecebimentoHistoricoLinhaUi(
    DateTime DataHoraValor,
    string UsuarioValor,
    string ProdutoValor,
    decimal QuantidadeValor,
    decimal? ValorPlanejadoNumero,
    decimal? ValorRecebidoNumero,
    bool DivergenciaValor,
    string ObservacaoValor)
{
    private static readonly CultureInfo Cultura = new("pt-BR");

    public string DataHora => DataHoraValor.ToString("dd/MM/yyyy HH:mm:ss");
    public string Usuario => UsuarioValor;
    public string Produto => ProdutoValor;
    public string Quantidade => QuantidadeValor.ToString("N3", Cultura);
    public string ValorPlanejado => ValorPlanejadoNumero.HasValue ? ValorPlanejadoNumero.Value.ToString("C2", Cultura) : "-";
    public string ValorRecebido => ValorRecebidoNumero.HasValue ? ValorRecebidoNumero.Value.ToString("C2", Cultura) : "-";
    public string Divergencia => DivergenciaValor ? "Sim" : "Nao";
    public decimal ImpactoDivergenciaNumero =>
        DivergenciaValor && ValorPlanejadoNumero.HasValue && ValorRecebidoNumero.HasValue
            ? Math.Abs(ValorRecebidoNumero.Value - ValorPlanejadoNumero.Value) * QuantidadeValor
            : 0m;
    public string ImpactoDivergencia => ImpactoDivergenciaNumero.ToString("C2", Cultura);
    public string Observacao => ObservacaoValor;
}

public sealed record RankingResumoLinhaUi(
    string Nome,
    string Entradas,
    string Quantidade,
    string Impacto);
