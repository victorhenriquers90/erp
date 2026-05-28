using System.Security.Claims;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Api.Services;

/// <summary>
/// Token service with Redis caching for improved performance.
/// Extends ITokenService with cached token validation to avoid repeated cryptographic operations.
/// </summary>
public interface ICachedTokenService
{
    /// <summary>
    /// Generate JWT access token for user.
    /// Token is cached in Redis for fast subsequent validations.
    /// </summary>
    string GenerateAccessToken(Usuario usuario);

    /// <summary>
    /// Generate JWT refresh token for user.
    /// </summary>
    string GenerateRefreshToken(Usuario usuario);

    /// <summary>
    /// Validate token against cached versions and cryptographic validation.
    /// Returns null if invalid, expired, or tampered.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>
    /// Extract user ID from token claims.
    /// Returns null if token is invalid.
    /// </summary>
    int? GetUserIdFromToken(string token);

    /// <summary>
    /// Extract user role from token claims.
    /// Returns null if token is invalid.
    /// </summary>
    string? GetRoleFromToken(string token);

    /// <summary>
    /// Invalidate cached token (for logout/revocation).
    /// </summary>
    Task InvalidateTokenAsync(string token);

    /// <summary>
    /// Get cache hit/miss statistics for monitoring.
    /// </summary>
    Task<(int Hits, int Misses)> GetCacheStatsAsync();
}
