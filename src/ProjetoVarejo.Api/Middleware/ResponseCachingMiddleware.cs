using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using ProjetoVarejo.Api.Configuration;
using StackExchange.Redis;
using Serilog;

namespace ProjetoVarejo.Api.Middleware;

/// <summary>
/// HTTP response caching middleware for GET endpoints.
/// Caches responses based on URL and query parameters.
/// Automatically invalidates cache on POST/PUT/DELETE operations.
/// </summary>
public class ResponseCachingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RedisSettings _redisSettings;
    private readonly CachingSettings _cachingSettings;
    private readonly IConnectionMultiplexer? _redis;

    public ResponseCachingMiddleware(
        RequestDelegate next,
        IOptions<RedisSettings> redisSettings,
        IOptions<CachingSettings> cachingSettings,
        IConnectionMultiplexer? redis = null)
    {
        _next = next;
        _redisSettings = redisSettings.Value;
        _cachingSettings = cachingSettings.Value;
        _redis = redis;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only cache GET requests for enabled endpoints
        if (context.Request.Method == "GET" && _redisSettings.Enabled && _redis != null)
        {
            await HandleGetRequestAsync(context);
        }
        else if (context.Request.Method is "POST" or "PUT" or "DELETE" && _redisSettings.Enabled && _redis != null)
        {
            // Invalidate related caches on write operations
            await InvalidateRelatedCachesAsync(context);
            await _next(context);
        }
        else
        {
            await _next(context);
        }
    }

    private async Task HandleGetRequestAsync(HttpContext context)
    {
        var cacheKey = GenerateCacheKey(context.Request);
        var db = _redis!.GetDatabase(_redisSettings.Database);

        try
        {
            // Check cache
            var cachedResponse = await db.StringGetAsync(cacheKey);

            if (cachedResponse.HasValue)
            {
                Log.Debug("Cache hit para {Path}", context.Request.Path);
                context.Response.ContentType = "application/json";
                context.Response.Headers["X-Cache"] = "HIT";
                await context.Response.WriteAsync(cachedResponse.ToString());
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Erro ao verificar cache para {Path}", context.Request.Path);
        }

        // Capture response body
        var originalBodyStream = context.Response.Body;
        using (var memoryStream = new MemoryStream())
        {
            context.Response.Body = memoryStream;
            context.Response.Headers["X-Cache"] = "MISS";

            // Continue pipeline
            await _next(context);

            // Check if response should be cached
            if (context.Response.StatusCode == 200 && ShouldCacheEndpoint(context.Request.Path))
            {
                // Read response body
                memoryStream.Position = 0;
                using (var reader = new StreamReader(memoryStream))
                {
                    var responseBody = await reader.ReadToEndAsync();
                    var contentLength = Encoding.UTF8.GetByteCount(responseBody);

                    // Cache if response is large enough
                    if (contentLength >= _cachingSettings.MinimumFileSizeToCompress)
                    {
                        try
                        {
                            var expiry = GetCacheDuration(context.Request.Path);
                            await db.StringSetAsync(cacheKey, responseBody, expiry);
                            Log.Debug("Resposta armazenada em cache para {Path} por {Duration}",
                                context.Request.Path, expiry);
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(ex, "Erro ao armazenar resposta em cache");
                        }
                    }

                    // Write response back
                    memoryStream.Position = 0;
                    await memoryStream.CopyToAsync(originalBodyStream);
                }
            }
            else
            {
                // Write response without caching
                memoryStream.Position = 0;
                await memoryStream.CopyToAsync(originalBodyStream);
            }
        }
    }

    private async Task InvalidateRelatedCachesAsync(HttpContext context)
    {
        var db = _redis!.GetDatabase(_redisSettings.Database);

        try
        {
            // Get the resource type from path (e.g., /api/vendas -> vendas)
            var pathSegments = context.Request.Path.Value?.Split('/') ?? Array.Empty<string>();
            var resourceType = pathSegments.Length > 2 ? pathSegments[2] : null;

            if (string.IsNullOrEmpty(resourceType)) return;

            // Invalidate all related list caches
            var pattern = $"cache:{resourceType}:*";
            var server = _redis.GetServer(_redis.GetEndPoints().First());

            var keysToDelete = server.Keys(_redisSettings.Database, pattern).ToList();
            if (keysToDelete.Count > 0)
            {
                await db.KeyDeleteAsync(keysToDelete.ToArray());
                Log.Information("Invalidado {Count} entradas de cache para {ResourceType}",
                    keysToDelete.Count, resourceType);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Erro ao invalidar cache");
        }
    }

    private static string GenerateCacheKey(HttpRequest request)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append("cache:");
        keyBuilder.Append(request.Path.Value);

        // Include query parameters in cache key
        if (!string.IsNullOrEmpty(request.QueryString.Value))
        {
            keyBuilder.Append("?");
            keyBuilder.Append(request.QueryString.Value);
        }

        // Hash to keep key reasonable length
        using (var sha = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(keyBuilder.ToString()));
            return $"cache:{Convert.ToBase64String(hash)[..16]}";
        }
    }

    private static bool ShouldCacheEndpoint(string path)
    {
        // Cache only specific list endpoints
        return path.Contains("/api/") && (
            path.Contains("/produtos") ||
            path.Contains("/categorias") ||
            path.Contains("/fornecedores") ||
            path.Contains("/clientes")) &&
            !path.Contains("/vendas") && // Don't cache transaction-sensitive endpoints
            !path.Contains("/caixa") &&
            !path.Contains("/financeiro");
    }

    private static TimeSpan GetCacheDuration(string path)
    {
        // Different cache durations per endpoint
        if (path.Contains("/produtos"))
            return TimeSpan.FromMinutes(30);
        if (path.Contains("/categorias"))
            return TimeSpan.FromMinutes(60);
        if (path.Contains("/fornecedores"))
            return TimeSpan.FromMinutes(60);
        if (path.Contains("/clientes"))
            return TimeSpan.FromMinutes(45);

        return TimeSpan.FromMinutes(30); // Default
    }
}
