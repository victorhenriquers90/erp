using Microsoft.AspNetCore.Mvc;
using ProjetoVarejo.Application.Contracts.Services;

namespace ProjetoVarejo.Api.Endpoints;

public static class EstoqueEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/estoque").WithTags("Estoque");

        g.MapGet("/abaixo-minimo", async ([FromServices] IEstoqueService svc) =>
        {
            var lista = await svc.ProdutosAbaixoMinimoAsync();
            return Results.Ok(lista.Select(p => new
            {
                p.Id, p.Codigo, p.Descricao, p.Estoque, p.EstoqueMinimo
            }));
        });

        g.MapGet("/movimentos", async ([FromServices] IEstoqueService svc, [FromQuery] int? produtoId, [FromQuery] DateTime? de, [FromQuery] DateTime? ate) =>
        {
            var lista = await svc.ListarMovimentosAsync(produtoId, de, ate);
            return Results.Ok(lista.Select(m => new
            {
                m.Id, m.CriadoEm,
                Produto = m.Produto.Descricao,
                Tipo = m.Tipo.ToString(),
                m.Quantidade, m.SaldoAnterior, m.SaldoAtual,
                m.Documento, m.Observacao,
                Usuario = m.Usuario.Nome
            }));
        });
    }
}
