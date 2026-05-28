using Microsoft.AspNetCore.Mvc;
using ProjetoVarejo.Application.Services;

namespace ProjetoVarejo.Api.Endpoints;

public static class ClienteEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/clientes").WithTags("Clientes");

        g.MapGet("/", async ([FromServices] ClienteService svc, [FromQuery] string? q) =>
        {
            var lista = await svc.ListarAsync(q);
            return Results.Ok(lista.Select(c => new
            {
                c.Id, c.Nome, c.CpfCnpj, c.Telefone, c.Email,
                c.Cidade, c.Uf
            }));
        });

        g.MapGet("/{id:int}", async (int id, [FromServices] ClienteService svc) =>
        {
            var c = await svc.BuscarPorIdAsync(id);
            return c == null ? Results.NotFound() : Results.Ok(c);
        });
    }
}
