using ProjetoVarejo.Application.Services;

namespace ProjetoVarejo.Api.Endpoints;

public static class RelatorioEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/relatorios").WithTags("Relatórios");

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
    }
}
