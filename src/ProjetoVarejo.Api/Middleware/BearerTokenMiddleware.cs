using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Api.Services;
using Serilog;

namespace ProjetoVarejo.Api.Middleware;

/// <summary>
/// Middleware for validating JWT bearer tokens in the Authorization header.
/// Extracts and validates JWT tokens, setting the HttpContext.User if valid.
/// Falls back to API Key authentication if no bearer token is present.
/// </summary>
public class BearerTokenMiddleware
{
    private readonly RequestDelegate _next;

    public BearerTokenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
    {
        const string authorizationHeader = "Authorization";
        const string bearerScheme = "Bearer ";

        var authHeader = context.Request.Headers[authorizationHeader].FirstOrDefault();

        // Check if Authorization header contains Bearer token
        if (!string.IsNullOrEmpty(authHeader) &&
            authHeader.StartsWith(bearerScheme, StringComparison.OrdinalIgnoreCase))
        {
            // Extract token from "Bearer <token>"
            var token = authHeader[bearerScheme.Length..].Trim();

            // If nothing follows "Bearer ", treat as unauthenticated and continue
            if (string.IsNullOrEmpty(token))
            {
                await _next(context);
                return;
            }

            // Validate token
            var principal = tokenService.ValidateToken(token);

            if (principal != null)
            {
                // Token is valid, set the user principal for authorization
                context.User = principal;
                var userName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "unknown";
                Log.Debug("JWT token validado para usuário {Usuario}", userName);
            }
            else
            {
                // Token is invalid or expired
                Log.Warning("JWT token inválido ou expirado");
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(ApiResponse.Error(
                    "Token JWT inválido ou expirado",
                    401));
                return;
            }
        }

        // Continue to next middleware (ApiKey middleware or endpoint handler)
        await _next(context);
    }
}
