using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ProjetoVarejo.Infrastructure.Reporting;

// DTOs de relatório — independentes dos services para não violar a direção de dependência
public record VendaDiariaDto(DateTime Dia, int Quantidade, decimal Total);
public record ProdutoRankingDto(string Codigo, string Descricao, decimal Quantidade, decimal Faturamento, string Classe);
public record FluxoCaixaDto(DateTime Dia, decimal Entradas, decimal Saidas, decimal Saldo);

/// <summary>DTO de filial para o relatório consolidado — não referencia Application.</summary>
public record FilialStatusDto(
    string Nome,
    bool Online,
    decimal VendasHoje,
    int PedidosHoje,
    bool CaixaAberto,
    decimal SaldoPrevisto,
    int ContasAtrasadas,
    string? UrlApi);

public class RelatorioExporter
{
    static RelatorioExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ─── PDF ────────────────────────────────────────────────────────────────

    public byte[] GerarVendasPorDiaPdf(IList<VendaDiariaDto> dados, DateTime de, DateTime ate)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                Cabecalho(page, "Vendas por Dia", de, ate);
                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(1); });
                    CabecalhoTabela(t, "Data", "Qtd Vendas", "Total (R$)");
                    foreach (var (item, idx) in dados.Select((d, i) => (d, i)))
                    {
                        var bg = idx % 2 == 0 ? "#FFFFFF" : "#F3F4F6";
                        t.Cell().Background(bg).Padding(5).Text(item.Dia.ToString("dd/MM/yyyy"));
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(item.Quantidade.ToString());
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(item.Total.ToString("C2"));
                    }
                    LinhaTotal(t, dados.Sum(d => d.Quantidade).ToString(), dados.Sum(d => d.Total).ToString("C2"));
                });
                Rodape(page);
            });
        }).GeneratePdf();
    }

    public byte[] GerarCurvaAbcPdf(IList<ProdutoRankingDto> dados, DateTime de, DateTime ate)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                Cabecalho(page, "Curva ABC de Produtos", de, ate);
                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(1); c.RelativeColumn(3); c.RelativeColumn(1);
                        c.RelativeColumn(1); c.RelativeColumn(1);
                    });
                    CabecalhoTabela(t, "Código", "Descrição", "Qtd Vendida", "Faturamento (R$)", "Classe");
                    foreach (var (item, idx) in dados.Select((d, i) => (d, i)))
                    {
                        var bg = item.Classe == "A" ? "#ECFDF5" : item.Classe == "B" ? "#FEF9C3" : idx % 2 == 0 ? "#FFFFFF" : "#F3F4F6";
                        t.Cell().Background(bg).Padding(5).Text(item.Codigo);
                        t.Cell().Background(bg).Padding(5).Text(item.Descricao);
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(item.Quantidade.ToString("N2"));
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(item.Faturamento.ToString("C2"));
                        t.Cell().Background(bg).Padding(5).AlignCenter().Text(item.Classe).Bold();
                    }
                });
                Rodape(page);
            });
        }).GeneratePdf();
    }

    public byte[] GerarFluxoCaixaPdf(IList<FluxoCaixaDto> dados, DateTime de, DateTime ate)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                Cabecalho(page, "Fluxo de Caixa", de, ate);
                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1);
                    });
                    CabecalhoTabela(t, "Data", "Entradas (R$)", "Saídas (R$)", "Saldo (R$)");
                    foreach (var (item, idx) in dados.Select((d, i) => (d, i)))
                    {
                        var bg = idx % 2 == 0 ? "#FFFFFF" : "#F3F4F6";
                        var saldoCor = item.Saldo >= 0 ? "#15803D" : "#DC2626";
                        t.Cell().Background(bg).Padding(5).Text(item.Dia.ToString("dd/MM/yyyy"));
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(item.Entradas.ToString("C2"));
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(item.Saidas.ToString("C2"));
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(item.Saldo.ToString("C2")).FontColor(saldoCor);
                    }
                    LinhaTotal(t,
                        dados.Sum(d => d.Entradas).ToString("C2"),
                        dados.Sum(d => d.Saidas).ToString("C2"),
                        dados.Sum(d => d.Saldo).ToString("C2"));
                });
                Rodape(page);
            });
        }).GeneratePdf();
    }

    public byte[] GerarConsolidadoPdf(IList<FilialStatusDto> dados)
    {
        var agora = DateTime.Now;
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);

                page.Header().Column(col =>
                {
                    col.Item().Text("Relatório Consolidado de Filiais").FontSize(16).Bold();
                    col.Item().Text($"Período: {agora:dd/MM/yyyy HH:mm}").FontSize(10).FontColor("#6B7280");
                    col.Item().Height(8);
                });

                page.Content().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);   // Filial
                        c.RelativeColumn(1);   // Status
                        c.RelativeColumn(1.5f); // Vendas Hoje
                        c.RelativeColumn(1);   // Pedidos
                        c.RelativeColumn(1);   // Caixa
                        c.RelativeColumn(1.5f); // Saldo Previsto
                        c.RelativeColumn(1);   // Atrasadas
                    });

                    CabecalhoTabela(t, "Filial", "Status", "Vendas Hoje", "Pedidos",
                                        "Caixa", "Saldo Previsto", "Atrasadas");

                    foreach (var f in dados)
                    {
                        var bg = f.Online ? "#ECFDF5" : "#FEF2F2";
                        t.Cell().Background(bg).Padding(5).Text(f.Nome);
                        t.Cell().Background(bg).Padding(5).AlignCenter()
                            .Text(f.Online ? "Online" : "Offline")
                            .FontColor(f.Online ? "#15803D" : "#DC2626").Bold();
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(f.VendasHoje.ToString("C2"));
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(f.PedidosHoje.ToString());
                        t.Cell().Background(bg).Padding(5).AlignCenter()
                            .Text(f.CaixaAberto ? "Aberto" : "Fechado");
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(f.SaldoPrevisto.ToString("C2"));
                        t.Cell().Background(bg).Padding(5).AlignRight().Text(f.ContasAtrasadas.ToString());
                    }

                    // linha TOTAL
                    t.Cell().Background("#E5E7EB").Padding(6).Text("TOTAL").Bold();
                    t.Cell().Background("#E5E7EB").Padding(6).Text(""); // Status
                    t.Cell().Background("#E5E7EB").Padding(6).AlignRight()
                        .Text(dados.Sum(d => d.VendasHoje).ToString("C2")).Bold();
                    t.Cell().Background("#E5E7EB").Padding(6).AlignRight()
                        .Text(dados.Sum(d => d.PedidosHoje).ToString()).Bold();
                    t.Cell().Background("#E5E7EB").Padding(6).Text(""); // Caixa
                    t.Cell().Background("#E5E7EB").Padding(6).AlignRight()
                        .Text(dados.Sum(d => d.SaldoPrevisto).ToString("C2")).Bold();
                    t.Cell().Background("#E5E7EB").Padding(6).AlignRight()
                        .Text(dados.Sum(d => d.ContasAtrasadas).ToString()).Bold();
                });

                Rodape(page);
            });
        }).GeneratePdf();
    }

    // ─── Excel ──────────────────────────────────────────────────────────────

    public byte[] GerarVendasPorDiaExcel(IList<VendaDiariaDto> dados, DateTime de, DateTime ate)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Vendas por Dia");
        CabecalhoExcel(ws, "Vendas por Dia", de, ate);
        var headers = new[] { "Data", "Qtd Vendas", "Total (R$)" };
        EscreverCabecalhoTabela(ws, 3, headers);
        int row = 4;
        foreach (var item in dados)
        {
            ws.Cell(row, 1).Value = item.Dia.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = item.Quantidade;
            ws.Cell(row, 3).Value = item.Total;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            row++;
        }
        LinhaRodapeExcel(ws, row, headers.Length,
            ("B", dados.Sum(d => d.Quantidade).ToString()),
            ("C", dados.Sum(d => d.Total).ToString("N2")));
        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] GerarCurvaAbcExcel(IList<ProdutoRankingDto> dados, DateTime de, DateTime ate)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Curva ABC");
        CabecalhoExcel(ws, "Curva ABC de Produtos", de, ate);
        var headers = new[] { "Código", "Descrição", "Qtd Vendida", "Faturamento (R$)", "Classe" };
        EscreverCabecalhoTabela(ws, 3, headers);
        int row = 4;
        foreach (var item in dados)
        {
            ws.Cell(row, 1).Value = item.Codigo;
            ws.Cell(row, 2).Value = item.Descricao;
            ws.Cell(row, 3).Value = item.Quantidade;
            ws.Cell(row, 4).Value = item.Faturamento;
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Value = item.Classe;
            var cor = item.Classe == "A" ? XLColor.LightGreen : item.Classe == "B" ? XLColor.LightYellow : XLColor.NoColor;
            if (cor != XLColor.NoColor)
                ws.Range(row, 1, row, 5).Style.Fill.BackgroundColor = cor;
            row++;
        }
        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] GerarFluxoCaixaExcel(IList<FluxoCaixaDto> dados, DateTime de, DateTime ate)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Fluxo de Caixa");
        CabecalhoExcel(ws, "Fluxo de Caixa", de, ate);
        var headers = new[] { "Data", "Entradas (R$)", "Saídas (R$)", "Saldo (R$)" };
        EscreverCabecalhoTabela(ws, 3, headers);
        int row = 4;
        foreach (var item in dados)
        {
            ws.Cell(row, 1).Value = item.Dia.ToString("dd/MM/yyyy");
            ws.Cell(row, 2).Value = item.Entradas;
            ws.Cell(row, 3).Value = item.Saidas;
            ws.Cell(row, 4).Value = item.Saldo;
            foreach (var col in new[] { 2, 3, 4 })
                ws.Cell(row, col).Style.NumberFormat.Format = "#,##0.00";
            if (item.Saldo < 0)
                ws.Cell(row, 4).Style.Font.FontColor = XLColor.Red;
            row++;
        }
        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] GerarConsolidadoExcel(IList<FilialStatusDto> dados)
    {
        var agora = DateTime.Now;
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Consolidado");

        // Título
        ws.Cell(1, 1).Value = "Relatório Consolidado de Filiais";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Gerado em: {agora:dd/MM/yyyy HH:mm}";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        // Cabeçalho da tabela
        var headers = new[] { "Filial", "Status", "Vendas Hoje", "Pedidos", "Caixa", "Saldo Previsto", "Atrasadas" };
        EscreverCabecalhoTabela(ws, 3, headers);

        int row = 4;
        foreach (var f in dados)
        {
            ws.Cell(row, 1).Value = f.Nome;
            ws.Cell(row, 2).Value = f.Online ? "Online" : "Offline";
            ws.Cell(row, 3).Value = f.VendasHoje;
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 4).Value = f.PedidosHoje;
            ws.Cell(row, 5).Value = f.CaixaAberto ? "Aberto" : "Fechado";
            ws.Cell(row, 6).Value = f.SaldoPrevisto;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Value = f.ContasAtrasadas;

            var corFundo = f.Online ? XLColor.FromHtml("#ECFDF5") : XLColor.FromHtml("#FEF2F2");
            ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = corFundo;
            ws.Cell(row, 2).Style.Font.FontColor = f.Online ? XLColor.FromHtml("#15803D") : XLColor.FromHtml("#DC2626");

            row++;
        }

        // Linha de total em negrito
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 3).Value = dados.Sum(d => d.VendasHoje);
        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 4).Value = dados.Sum(d => d.PedidosHoje);
        ws.Cell(row, 6).Value = dados.Sum(d => d.SaldoPrevisto);
        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
        ws.Cell(row, 7).Value = dados.Sum(d => d.ContasAtrasadas);
        ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.LightGray;
        ws.Range(row, 1, row, headers.Length).Style.Font.Bold = true;

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static void Cabecalho(PageDescriptor page, string titulo, DateTime de, DateTime ate)
    {
        page.Header().Column(col =>
        {
            col.Item().Text(titulo).FontSize(16).Bold();
            col.Item().Text($"Período: {de:dd/MM/yyyy} a {ate:dd/MM/yyyy}").FontSize(10).FontColor("#6B7280");
            col.Item().Height(8);
        });
    }

    private static void Rodape(PageDescriptor page)
    {
        page.Footer().AlignRight()
            .Text($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).FontColor("#9CA3AF");
    }

    private static void CabecalhoTabela(TableDescriptor t, params string[] colunas)
    {
        foreach (var col in colunas)
            t.Cell().Background("#1E3A5F").Padding(6).Text(col).FontColor("#FFFFFF").Bold();
    }

    private static void LinhaTotal(TableDescriptor t, params string[] valores)
    {
        foreach (var v in valores)
            t.Cell().Background("#E5E7EB").Padding(6).AlignRight().Text(v).Bold();
    }

    private static void CabecalhoExcel(IXLWorksheet ws, string titulo, DateTime de, DateTime ate)
    {
        ws.Cell(1, 1).Value = titulo;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Período: {de:dd/MM/yyyy} a {ate:dd/MM/yyyy}";
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;
    }

    private static void EscreverCabecalhoTabela(IXLWorksheet ws, int row, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(row, i + 1).Value = headers[i];
            ws.Cell(row, i + 1).Style.Font.Bold = true;
            ws.Cell(row, i + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A5F");
            ws.Cell(row, i + 1).Style.Font.FontColor = XLColor.White;
        }
    }

    private static void LinhaRodapeExcel(IXLWorksheet ws, int row, int cols, params (string col, string val)[] totais)
    {
        ws.Cell(row, 1).Value = "TOTAL";
        ws.Cell(row, 1).Style.Font.Bold = true;
        foreach (var (col, val) in totais)
            ws.Cell($"{col}{row}").Value = val;
        ws.Range(row, 1, row, cols).Style.Fill.BackgroundColor = XLColor.LightGray;
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
