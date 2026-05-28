using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Api.Services;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Shared;
using Serilog;

namespace ProjetoVarejo.Api.Endpoints;

/// <summary>
/// API endpoints for authentication.
/// Handles login and token management.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth")
            .WithName("Auth")
            .WithOpenApi()
            .WithTags("Auth");

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithDescription("Autentica um usuário e retorna credenciais");

        group.MapPost("/refresh", Refresh)
            .WithName("Refresh")
            .WithDescription("Atualiza o token de autenticação (PHASE 8)")
            .RequireAuthorization();
    }

    /// <summary>
    /// POST /api/auth/login - Authenticate user with credentials and return JWT tokens
    /// </summary>
    private static async Task<IResult> Login(
        LoginRequest request,
        IAutenticacaoService autenticacaoService,
        ITokenService tokenService,
        [Microsoft.AspNetCore.Mvc.FromServices] IUnitOfWork unitOfWork)
    {
        try
        {
            Log.Information("Tentativa de login para usuário {Usuario}", request.Usuario);

            if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Senha))
            {
                return Results.BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Usuário e senha são obrigatórios",
                    ErrorCode = 400
                });
            }

            var resultado = await autenticacaoService.LoginAsync(request.Usuario, request.Senha);

            if (!resultado.Sucesso)
            {
                Log.Warning("Falha na autenticação para usuário {Usuario}", request.Usuario);
                return Results.BadRequest(ApiResponse.Error("Usuário ou senha inválidos", 401));
            }

            var usuario = resultado.Valor!;

            // Build permission set: profile defaults + any custom grants stored per-user
            var perfilPadrao = PermissaoService.PermissoesPadrao.TryGetValue(usuario.Perfil, out var pp) ? pp : new();
            var custom = await unitOfWork.UsuarioPermissoes.Query()
                .Where(p => p.UsuarioId == usuario.Id)
                .Select(p => p.Permissao)
                .ToListAsync();
            var permissoes = new HashSet<Permissao>(perfilPadrao.Concat(custom));

            // Generate JWT tokens (access token with full permission claims)
            var accessToken = tokenService.GenerateAccessToken(usuario, permissoes);
            var refreshToken = tokenService.GenerateRefreshToken(usuario);

            var response = new LoginResponse
            {
                UsuarioId = usuario.Id,
                UsuarioNome = usuario.Nome,
                UsuarioPerfil = usuario.Perfil.ToString(),
                ApiKey = "legacy-api-key", // Legacy API Key (deprecated, for backward compatibility)
                Token = accessToken,        // JWT access token
                RefreshToken = refreshToken, // JWT refresh token
                ExpiresIn = 3600            // 1 hour in seconds
            };

            Log.Information("Login bem-sucedido para usuário {Usuario}", request.Usuario);
            return Results.Ok(new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = response,
                Message = "Login realizado com sucesso"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao processar login");
            return Results.BadRequest(ApiResponse.Error("Erro ao processar login: " + ex.Message, 400));
        }
    }

    /// <summary>
    /// POST /api/auth/refresh - Refresh authentication token using refresh token
    /// </summary>
    private static async Task<IResult> Refresh(
        RefreshRequest request,
        ITokenService tokenService,
        [Microsoft.AspNetCore.Mvc.FromServices] IUnitOfWork unitOfWork)
    {
        try
        {
            Log.Information("Refresh de token solicitado");

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                return Results.BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Refresh token é obrigatório",
                    ErrorCode = 400
                });
            }

            // Validate refresh token signature and expiry
            var principal = tokenService.ValidateToken(request.RefreshToken);
            if (principal == null)
            {
                Log.Warning("Refresh token inválido");
                return Results.BadRequest(ApiResponse.Error("Refresh token inválido ou expirado", 401));
            }

            // Extract user ID from refresh token claims
            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                Log.Warning("Refresh token não contém user ID válido");
                return Results.BadRequest(ApiResponse.Error("Refresh token inválido", 400));
            }

            // Load user from database to get current permissions and profile
            var usuario = await unitOfWork.Usuarios.Query()
                .FirstOrDefaultAsync(u => u.Id == userId && u.Ativo);

            if (usuario == null)
            {
                Log.Warning("Usuário {UserId} não encontrado ou inativo durante refresh", userId);
                return Results.BadRequest(ApiResponse.Error("Usuário não encontrado ou inativo", 401));
            }

            // Rebuild full permission set: profile defaults + custom grants
            var perfilPadrao = PermissaoService.PermissoesPadrao.TryGetValue(usuario.Perfil, out var pp) ? pp : new();
            var custom = await unitOfWork.UsuarioPermissoes.Query()
                .Where(p => p.UsuarioId == usuario.Id)
                .Select(p => p.Permissao)
                .ToListAsync();
            var permissoes = new HashSet<Permissao>(perfilPadrao.Concat(custom));
            var newAccessToken = tokenService.GenerateAccessToken(usuario, permissoes);

            Log.Information("Token renovado com sucesso para usuário {Login}", usuario.Login);

            return Results.Ok(new ApiResponse<RefreshResponse>
            {
                Success = true,
                Data = new RefreshResponse
                {
                    Token = newAccessToken,
                    ExpiresIn = 3600
                },
                Message = "Token atualizado com sucesso"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao atualizar token");
            return Results.BadRequest(ApiResponse.Error("Erro ao atualizar token: " + ex.Message, 400));
        }
    }
}

/// <summary>Request model for login.</summary>
public class LoginRequest
{
    /// <summary>Gets or sets the username.</summary>
    public string Usuario { get; set; } = string.Empty;

    /// <summary>Gets or sets the password.</summary>
    public string Senha { get; set; } = string.Empty;
}

/// <summary>Response model for successful login.</summary>
public class LoginResponse
{
    /// <summary>Gets or sets the user ID.</summary>
    public int UsuarioId { get; set; }

    /// <summary>Gets or sets the user name.</summary>
    public string UsuarioNome { get; set; } = string.Empty;

    /// <summary>Gets or sets the user role/profile.</summary>
    public string UsuarioPerfil { get; set; } = string.Empty;

    /// <summary>Gets or sets the API key (legacy, for backward compatibility).</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the JWT access token.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Gets or sets the JWT refresh token.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Gets or sets the token expiration time in seconds.</summary>
    public int ExpiresIn { get; set; }
}

/// <summary>Request model for token refresh.</summary>
public class RefreshRequest
{
    /// <summary>Gets or sets the refresh token.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>Response model for token refresh.</summary>
public class RefreshResponse
{
    /// <summary>Gets or sets the new JWT token.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Gets or sets the token expiration time in seconds.</summary>
    public int ExpiresIn { get; set; }
}
