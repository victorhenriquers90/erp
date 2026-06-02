using ProjetoVarejo.Application.Services;

namespace ProjetoVarejo.Api.Endpoints;

public static class ProdutoEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/produtos").WithTags("Produtos");

        g.MapGet("/", async (ProdutoService svc, string? q) =>
        {
            var lista = await svc.ListarAsync(q);
            return Results.Ok(lista.Select(p => new
            {
                p.Id, p.Codigo, p.CodigoBarras, p.Descricao,
                Categoria = p.Categoria?.Nome,
                Unidade = p.Unidade.ToString(),
                p.PrecoVenda, p.Estoque, p.EstoqueMinimo, p.Ativo
            }));
        });

        g.MapGet("/{id:int}", async (int id, ProdutoService svc) =>
        {
            var p = await svc.BuscarPorIdAsync(id);
            return p == null ? Results.NotFound() : Results.Ok(p);
        });

        g.MapGet("/barras/{codigo}", async (string codigo, ProdutoService svc) =>
        {
            var p = await svc.BuscarPorCodigoAsync(codigo);
            if (p == null) return Results.NotFound(new { erro = "Produto não encontrado." });
            return Results.Ok(new
            {
                p.Id, p.Codigo, p.CodigoBarras, p.Descricao,
                p.PrecoVenda, p.PrecoCusto, p.Estoque,
                Unidade = p.Unidade.ToString()
            });
        });
    }
}
