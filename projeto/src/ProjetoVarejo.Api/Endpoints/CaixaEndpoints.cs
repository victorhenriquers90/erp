using ProjetoVarejo.Application.Services;

namespace ProjetoVarejo.Api.Endpoints;

public static class CaixaEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/caixa").WithTags("Caixa");

        g.MapGet("/aberto", async (CaixaService svc) =>
        {
            var caixa = await svc.ObterCaixaAbertoAsync();
            if (caixa == null) return Results.Ok(new { aberto = false });
            return Results.Ok(new { aberto = true, caixa.Id, caixa.AbertaEm, caixa.ValorAbertura });
        });

        g.MapPost("/abrir", async (AbrirCaixaRequest req, CaixaService svc) =>
        {
            var result = await svc.AbrirAsync(req.ValorAbertura);
            if (!result.Sucesso) return Results.BadRequest(new { erro = result.Erro });
            var c = result.Valor!;
            return Results.Ok(new { c.Id, c.AbertaEm, c.ValorAbertura });
        });

        g.MapPost("/sangria", async (MovimentoRequest req, CaixaService svc) =>
        {
            var result = await svc.SangriaAsync(req.Valor, req.Motivo);
            if (!result.Sucesso) return Results.BadRequest(new { erro = result.Erro });
            var m = result.Valor!;
            return Results.Ok(new { m.Id, m.Data, m.Valor, m.Observacao });
        });

        g.MapPost("/suprimento", async (MovimentoRequest req, CaixaService svc) =>
        {
            var result = await svc.SuprimentoAsync(req.Valor, req.Motivo);
            if (!result.Sucesso) return Results.BadRequest(new { erro = result.Erro });
            var m = result.Valor!;
            return Results.Ok(new { m.Id, m.Data, m.Valor, m.Observacao });
        });

        g.MapGet("/{id:int}/resumo", async (int id, CaixaService svc) =>
        {
            var resumo = await svc.ResumoAsync(id);
            return Results.Ok(new
            {
                resumo.ValorAbertura,
                resumo.TotalSuprimentos,
                resumo.TotalSangrias,
                resumo.TotalVendas,
                resumo.SaldoDinheiroEsperado,
                VendasPorForma = resumo.VendasPorForma.ToDictionary(k => k.Key.ToString(), k => k.Value)
            });
        });

        g.MapPost("/{id:int}/fechar", async (int id, FecharCaixaRequest req, CaixaService svc) =>
        {
            var result = await svc.FecharAsync(id, req.ValorInformado, req.Observacao);
            if (!result.Sucesso) return Results.BadRequest(new { erro = result.Erro });
            var c = result.Valor!;
            return Results.Ok(new
            {
                c.Id, c.FechadaEm,
                c.ValorFechamentoInformado, c.ValorFechamentoCalculado, c.Diferenca
            });
        });
    }

    public record AbrirCaixaRequest(decimal ValorAbertura);
    public record MovimentoRequest(decimal Valor, string Motivo);
    public record FecharCaixaRequest(decimal ValorInformado, string? Observacao);
}
