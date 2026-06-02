using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Infrastructure.Data;

namespace ProjetoVarejo.Api.Endpoints;

public static class UsuarioEndpoints
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/auth/login", async (LoginRequest req, AutenticacaoService svc) =>
        {
            var result = await svc.LoginAsync(req.Login, req.Senha);
            if (!result.Sucesso) return Results.Unauthorized();
            var u = result.Valor!;
            return Results.Ok(new
            {
                u.Id, u.Login, u.Nome,
                Perfil = u.Perfil.ToString(),
                u.UltimoAcesso
            });
        }).WithTags("Auth").AllowAnonymous();

        app.MapGet("/api/usuarios", async (AppDbContext db) =>
        {
            var lista = await db.Usuarios
                .Where(u => u.Ativo)
                .OrderBy(u => u.Nome)
                .Select(u => new
                {
                    u.Id, u.Login, u.Nome,
                    Perfil = u.Perfil.ToString(),
                    u.UltimoAcesso
                })
                .ToListAsync();
            return Results.Ok(lista);
        }).WithTags("Auth");
    }

    public record LoginRequest(string Login, string Senha);
}
