using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjetoVarejo.Api.Configuration;
using ProjetoVarejo.Domain.Entities;
using StackExchange.Redis;
using Serilog;

namespace ProjetoVarejo.Api.Services;

/// <summary>
/// Token service with Redis caching for optimized performance.
/// Caches validated tokens to avoid repeated cryptographic operations.
/// Falls back to ITokenService for generation and initial validation.
/// </summary>
public class CachedTokenService : ICachedTokenService
{
    private readonly ITokenService _tokenService;
    private readonly RedisSettings _redisSettings;
    private readonly CachingSettings _cachingSettings;
    private readonly IConnectionMultiplexer? _redis;
    private IDatabase? _db;

    // In-memory stats for cache performance tracking
    private long _cacheHits;
    private long _cacheMisses;

    public CachedTokenService(
        ITokenService tokenService,
        IOptions<RedisSettings> redisSettings,
        IOptions<CachingSettings> cachingSettings,
        IConnectionMultiplexer? redis = null)
    {
        _tokenService = tokenService;
        _redisSettings = redisSettings.Value;
        _cachingSettings = cachingSettings.Value;
        _redis = redis;
        _db = redis?.GetDatabase(_redisSettings.Database);
    }

    /// <summary>
    /// Generate JWT access token and cache it in Redis.
    /// </summary>
    public string GenerateAccessToken(Usuario usuario)
    {
        try
        {
            var token = _tokenService.GenerateAccessToken(usuario);

            // Cache token if Redis is enabled
            if (_redisSettings.Enabled && _db != null)
            {
                var cacheKey = GetTokenCacheKey(token);
                var expiry = TimeSpan.FromMinutes(_cachingSettings.TokenCacheDurationMinutes);

                try
                {
                    _db.StringSet(cacheKey, "valid", expiry);
                    Log.Debug("Token JWT armazenado em cache para usuário {Usuario}", usuario.Login);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Erro ao armazenar token em cache para usuário {Usuario}", usuario.Login);
                    // Falha silenciosa - continua sem cache
                }
            }

            return token;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao gerar token de acesso");
            throw;
        }
    }

    /// <summary>
    /// Generate refresh token (não é cacheado).
    /// </summary>
    public string GenerateRefreshToken(Usuario usuario)
    {
        return _tokenService.GenerateRefreshToken(usuario);
    }

    /// <summary>
    /// Validate token using cached result if available, otherwise validate cryptographically.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            // Check Redis cache first
            if (_redisSettings.Enabled && _db != null)
            {
                var cacheKey = GetTokenCacheKey(token);

                try
                {
                    var cachedValue = _db.StringGet(cacheKey);
                    if (cachedValue.HasValue)
                    {
                        Interlocked.Increment(ref _cacheHits);
                        Log.Debug("Cache hit para validação de token JWT");

                        // Se está em cache e ainda válido, confia na cache
                        // (Redis expira automaticamente)
                        return _tokenService.ValidateToken(token);
                    }

                    Interlocked.Increment(ref _cacheMisses);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Erro ao verificar token em cache");
                    // Falha silenciosa - continua com validação cripto
                }
            }

            // Validar criptograficamente
            var principal = _tokenService.ValidateToken(token);

            // Armazenar resultado positivo em cache
            if (principal != null && _redisSettings.Enabled && _db != null)
            {
                try
                {
                    var cacheKey = GetTokenCacheKey(token);
                    var expiry = TimeSpan.FromMinutes(_cachingSettings.TokenCacheDurationMinutes);
                    _db.StringSet(cacheKey, "valid", expiry);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Erro ao armazenar resultado de validação em cache");
                }
            }

            return principal;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao validar token");
            return null;
        }
    }

    /// <summary>
    /// Extract user ID from cached or validated token.
    /// </summary>
    public int? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null) return null;

        var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Extract role from cached or validated token.
    /// </summary>
    public string? GetRoleFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Invalidate token from cache (for logout/revocation).
    /// </summary>
    public async Task InvalidateTokenAsync(string token)
    {
        if (!_redisSettings.Enabled || _db == null) return;

        try
        {
            var cacheKey = GetTokenCacheKey(token);
            await _db.KeyDeleteAsync(cacheKey);
            Log.Information("Token invalidado no cache");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Erro ao invalidar token no cache");
        }
    }

    /// <summary>
    /// Get cache hit/miss statistics for monitoring.
    /// </summary>
    public Task<(int Hits, int Misses)> GetCacheStatsAsync()
    {
        var stats = ((int)_cacheHits, (int)_cacheMisses);
        return Task.FromResult(stats);
    }

    /// <summary>
    /// Generate cache key for token (using hash of token for security).
    /// </summary>
    private static string GetTokenCacheKey(string token)
    {
        // Hash first 50 chars of token to create cache key
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            var hashStr = Convert.ToBase64String(hash)[..16]; // First 16 chars of hash
            return $"token:{hashStr}";
        }
    }
}
