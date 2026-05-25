using Microsoft.Extensions.Configuration;

namespace ProjetoVarejo.Api.Auth;

public class ApiKeyValidator
{
    private readonly HashSet<string> _chaves;

    public ApiKeyValidator(IConfiguration config)
    {
        var chaves = config.GetSection("ApiKeys").Get<string[]>() ?? Array.Empty<string>();
        _chaves = new HashSet<string>(chaves, StringComparer.Ordinal);
    }

    public bool Valida(string? chave) =>
        !string.IsNullOrWhiteSpace(chave) && _chaves.Contains(chave);
}

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    public ApiKeyMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx, ApiKeyValidator validator)
    {
        var path = ctx.Request.Path.Value ?? "";
        if (path == "/" || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        var chave = ctx.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (!validator.Valida(chave))
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new { erro = "API key inválida ou ausente. Envie no header X-Api-Key." });
            return;
        }
        await _next(ctx);
    }
}
