using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Infrastructure.Reporting;

namespace ProjetoVarejo.Api.Endpoints;

public static class RelatorioEndpoints
{
    private const string PdfType = "application/pdf";
    private const string ExcelType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/relatorios").WithTags("Relatórios");

        // ─── JSON
        g.MapGet("/vendas-por-dia", async (RelatorioService svc, DateTime de, DateTime ate) =>
            Results.Ok(await svc.VendasPorDiaAsync(de, ate)));

        g.MapGet("/por-forma-pagamento", async (RelatorioService svc, DateTime de, DateTime ate) =>
            Results.Ok(await svc.VendasPorFormaPagamentoAsync(de, ate)));

        g.MapGet("/top-produtos", async (RelatorioService svc, DateTime de, DateTime ate, int? n) =>
            Results.Ok(await svc.TopProdutosAsync(de, ate, n ?? 20)));

        g.MapGet("/curva-abc", async (RelatorioService svc, DateTime de, DateTime ate) =>
            Results.Ok(await svc.CurvaAbcAsync(de, ate)));

        g.MapGet("/fluxo-caixa", async (RelatorioService svc, DateTime de, DateTime ate) =>
            Results.Ok(await svc.FluxoCaixaAsync(de, ate)));

        // ─── PDF
        g.MapGet("/vendas-por-dia/pdf", async (RelatorioService svc, RelatorioExporter exp, DateTime de, DateTime ate) =>
        {
            var dados = (await svc.VendasPorDiaAsync(de, ate))
                .Select(x => new VendaDiariaDto(x.Dia, x.Quantidade, x.Total)).ToList();
            return Results.File(exp.GerarVendasPorDiaPdf(dados, de, ate), PdfType, $"vendas-{de:yyyyMMdd}-{ate:yyyyMMdd}.pdf");
        });

        g.MapGet("/curva-abc/pdf", async (RelatorioService svc, RelatorioExporter exp, DateTime de, DateTime ate) =>
        {
            var dados = (await svc.CurvaAbcAsync(de, ate))
                .Select(x => new ProdutoRankingDto(x.Codigo, x.Descricao, x.Quantidade, x.Faturamento, x.Classe)).ToList();
            return Results.File(exp.GerarCurvaAbcPdf(dados, de, ate), PdfType, $"curva-abc-{de:yyyyMMdd}-{ate:yyyyMMdd}.pdf");
        });

        g.MapGet("/fluxo-caixa/pdf", async (RelatorioService svc, RelatorioExporter exp, DateTime de, DateTime ate) =>
        {
            var dados = (await svc.FluxoCaixaAsync(de, ate))
                .Select(x => new FluxoCaixaDto(x.Dia, x.Entradas, x.Saidas, x.Saldo)).ToList();
            return Results.File(exp.GerarFluxoCaixaPdf(dados, de, ate), PdfType, $"fluxo-caixa-{de:yyyyMMdd}-{ate:yyyyMMdd}.pdf");
        });

        // ─── Excel
        g.MapGet("/vendas-por-dia/excel", async (RelatorioService svc, RelatorioExporter exp, DateTime de, DateTime ate) =>
        {
            var dados = (await svc.VendasPorDiaAsync(de, ate))
                .Select(x => new VendaDiariaDto(x.Dia, x.Quantidade, x.Total)).ToList();
            return Results.File(exp.GerarVendasPorDiaExcel(dados, de, ate), ExcelType, $"vendas-{de:yyyyMMdd}-{ate:yyyyMMdd}.xlsx");
        });

        g.MapGet("/curva-abc/excel", async (RelatorioService svc, RelatorioExporter exp, DateTime de, DateTime ate) =>
        {
            var dados = (await svc.CurvaAbcAsync(de, ate))
                .Select(x => new ProdutoRankingDto(x.Codigo, x.Descricao, x.Quantidade, x.Faturamento, x.Classe)).ToList();
            return Results.File(exp.GerarCurvaAbcExcel(dados, de, ate), ExcelType, $"curva-abc-{de:yyyyMMdd}-{ate:yyyyMMdd}.xlsx");
        });

        g.MapGet("/fluxo-caixa/excel", async (RelatorioService svc, RelatorioExporter exp, DateTime de, DateTime ate) =>
        {
            var dados = (await svc.FluxoCaixaAsync(de, ate))
                .Select(x => new FluxoCaixaDto(x.Dia, x.Entradas, x.Saidas, x.Saldo)).ToList();
            return Results.File(exp.GerarFluxoCaixaExcel(dados, de, ate), ExcelType, $"fluxo-caixa-{de:yyyyMMdd}-{ate:yyyyMMdd}.xlsx");
        });
    }
}
