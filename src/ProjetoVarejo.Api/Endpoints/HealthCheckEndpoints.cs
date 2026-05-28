using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProjetoVarejo.Api.Configuration;
using Serilog;

namespace ProjetoVarejo.Api.Endpoints;

/// <summary>
/// Health check endpoints for Kubernetes probes and monitoring.
/// - GET /health - Overall application health
/// - GET /health/ready - Readiness probe (can handle requests)
/// - GET /health/live - Liveness probe (process is alive)
/// </summary>
public static class HealthCheckEndpoints
{
    public static void MapHealthCheckEndpoints(this WebApplication app)
    {
        // Map individual health check endpoints
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            ResponseWriter = WriteHealthCheckResponseAsync,
            AllowCachingResponses = false
        })
        .WithName("HealthCheck")
        .WithOpenApi()
        .AllowAnonymous();

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthCheckResponseAsync,
            AllowCachingResponses = false
        })
        .WithName("ReadinessProbe")
        .WithOpenApi()
        .AllowAnonymous();

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live"),
            ResponseWriter = WriteHealthCheckResponseAsync,
            AllowCachingResponses = false
        })
        .WithName("LivenessProbe")
        .WithOpenApi()
        .AllowAnonymous();
    }

    public static void AddHealthCheckServices(this WebApplicationBuilder builder)
    {
        var healthChecks = builder.Services.AddHealthChecks();

        // Add database health check (required for readiness)
        healthChecks.AddDbContextCheck<ProjetoVarejo.Infrastructure.Data.AppDbContext>(
            name: "database",
            tags: new[] { "ready", "live" });

        // Add Redis health check (if enabled)
        var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>();
        if (redisSettings?.Enabled == true)
        {
            healthChecks.AddRedis(
                redisSettings.Connection,
                name: "redis",
                tags: new[] { "ready" });
        }

        // Add custom health checks
        healthChecks.AddCheck("memory", new MemoryHealthCheck(), tags: new[] { "live" });
        healthChecks.AddCheck("diskspace", new DiskSpaceHealthCheck(), tags: new[] { "live" });
    }

    private static async Task WriteHealthCheckResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow.ToString("O"),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    duration = entry.Value.Duration.TotalMilliseconds,
                    description = entry.Value.Description,
                    error = entry.Value.Exception?.Message
                })
        };

        // Set HTTP status code based on health
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 :
                                      report.Status == HealthStatus.Degraded ? 200 :
                                      503;

        await context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Health check for available memory (should be < 80%).
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var totalMemory = GC.GetTotalMemory(false);
            var workingSet = process.WorkingSet64;

            // Consider memory usage relative to total system memory
            var totalSystemMemory = GC.GetGCMemoryInfo().TotalCommittedBytes;

            // Unhealthy acima de 800MB, Degraded entre 500MB e 800MB
            var mb = workingSet / 1024 / 1024;
            if (workingSet > 800_000_000)
                return Task.FromResult(HealthCheckResult.Unhealthy($"Memória crítica: {mb}MB"));

            if (workingSet > 500_000_000)
                return Task.FromResult(HealthCheckResult.Degraded($"Memória elevada: {mb}MB"));

            return Task.FromResult(HealthCheckResult.Healthy($"Memória: {mb}MB"));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error checking memory health");
            return Task.FromResult(HealthCheckResult.Unhealthy("Memory check failed"));
        }
    }
}

/// <summary>
/// Health check for available disk space (should be > 1GB free).
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var drive = new DriveInfo(AppDomain.CurrentDomain.BaseDirectory);
            const long minimumFreeBytes = 1_000_000_000; // 1 GB

            if (drive.AvailableFreeSpace < minimumFreeBytes)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Low disk space: {drive.AvailableFreeSpace / 1024 / 1024}MB free"));
            }

            if (drive.AvailableFreeSpace < minimumFreeBytes * 3)
            {
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Disk space degraded: {drive.AvailableFreeSpace / 1024 / 1024}MB free"));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Disk space: {drive.AvailableFreeSpace / 1024 / 1024}MB free"));
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error checking disk health");
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk check failed"));
        }
    }
}
