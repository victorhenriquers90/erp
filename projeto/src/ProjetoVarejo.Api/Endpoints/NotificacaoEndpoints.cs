using ProjetoVarejo.Application.Services;

namespace ProjetoVarejo.Api.Endpoints;

public static class NotificacaoEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/notificacoes").WithTags("Notificações");

        g.MapPost("/cobrancas-vencidas", async (NotificacaoService svc) =>
        {
            var enviadas = await svc.EnviarCobrancasVencidasAsync();
            return Results.Ok(new { enviadas, mensagem = $"{enviadas} notificação(ões) enviada(s)." });
        });

        g.MapPost("/pix", async (PixRequest req, NotificacaoService svc) =>
        {
            var resultado = await svc.EnviarPixAsync(req.Telefone, req.Valor, req.BrCode);
            if (!resultado.Sucesso)
                return Results.BadRequest(new { erro = resultado.Erro });
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/confirmacao-venda/{id:int}", async (int id, NotificacaoService svc) =>
        {
            var ok = await svc.EnviarConfirmacaoVendaAsync(id);
            return ok
                ? Results.Ok(new { ok = true })
                : Results.BadRequest(new { erro = "Cliente sem telefone cadastrado ou venda não encontrada." });
        });
    }

    public record PixRequest(string Telefone, decimal Valor, string BrCode);
}
