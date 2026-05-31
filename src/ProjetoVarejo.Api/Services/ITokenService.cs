using System.Security.Claims;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Api.Services;

/// <summary>
/// Service for generating and validating JWT tokens.
/// Handles access tokens and refresh tokens for authentication.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generate a JWT access token for the given user.
    /// </summary>
    string GenerateAccessToken(Usuario usuario);

    /// <summary>
    /// Generate a JWT access token with explicit permission claims embedded.
    /// Use this overload when permissions have already been loaded from the database.
    /// </summary>
    string GenerateAccessToken(Usuario usuario, IEnumerable<Permissao> permissoes);

    /// <summary>
    /// Generate a refresh token for the given user.
    /// </summary>
    /// <param name="usuario">The user entity to create a refresh token for</param>
    /// <returns>JWT refresh token string</returns>
    string GenerateRefreshToken(Usuario usuario);

    /// <summary>
    /// Validate a JWT token and return the principal if valid.
    /// </summary>
    /// <param name="token">The JWT token to validate</param>
    /// <returns>ClaimsPrincipal if valid, null if invalid or expired</returns>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Get user ID from token claims.
    /// </summary>
    /// <param name="token">The JWT token to extract user ID from</param>
    /// <returns>User ID if found, null otherwise</returns>
    int? GetUserIdFromToken(string token);

    /// <summary>
    /// Get user role from token claims.
    /// </summary>
    /// <param name="token">The JWT token to extract role from</param>
    /// <returns>Role (perfil) string if found, null otherwise</returns>
    string? GetRoleFromToken(string token);
}
