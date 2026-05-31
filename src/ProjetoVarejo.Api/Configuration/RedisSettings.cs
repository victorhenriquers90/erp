namespace ProjetoVarejo.Api.Configuration;

/// <summary>
/// Configuration for Redis caching service.
/// Binds to "Redis" section in appsettings.json.
/// </summary>
public class RedisSettings
{
    /// <summary>Redis connection string (host:port)</summary>
    public string Connection { get; set; } = "localhost:6379";

    /// <summary>Redis database number (0-15)</summary>
    public int Database { get; set; } = 0;

    /// <summary>Enable/disable Redis caching</summary>
    public bool Enabled { get; set; } = false;
}

/// <summary>
/// Configuration for application caching behavior.
/// Binds to "Caching" section in appsettings.json.
/// </summary>
public class CachingSettings
{
    /// <summary>JWT token cache duration in minutes</summary>
    public int TokenCacheDurationMinutes { get; set; } = 60;

    /// <summary>Product list cache duration in minutes</summary>
    public int ProductListCacheDurationMinutes { get; set; } = 30;

    /// <summary>Category list cache duration in minutes</summary>
    public int CategoryListCacheDurationMinutes { get; set; } = 60;

    /// <summary>Supplier list cache duration in minutes</summary>
    public int SupplierListCacheDurationMinutes { get; set; } = 60;

    /// <summary>Minimum file size in bytes before compression is applied</summary>
    public int MinimumFileSizeToCompress { get; set; } = 1024;
}

/// <summary>
/// Configuration for health checks.
/// Binds to "HealthChecks" section in appsettings.json.
/// </summary>
public class HealthCheckSettings
{
    /// <summary>Database connectivity check timeout in seconds</summary>
    public int DatabaseTimeoutSeconds { get; set; } = 10;

    /// <summary>Redis connectivity check timeout in seconds</summary>
    public int RedisTimeoutSeconds { get; set; } = 5;
}
