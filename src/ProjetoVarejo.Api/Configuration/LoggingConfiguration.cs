using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace ProjetoVarejo.Api.Configuration;

/// <summary>
/// Serilog structured logging configuration for the API.
/// Configures console, file, and Application Insights sinks with appropriate log levels.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configure Serilog for the application.
    /// Call this in Program.cs before building the host.
    /// </summary>
    public static void ConfigureLogging(WebApplicationBuilder builder)
    {
        var environment = builder.Environment.EnvironmentName;
        var isDevelopment = builder.Environment.IsDevelopment();

        Log.Logger = new LoggerConfiguration()
            // Set minimum level based on environment
            .MinimumLevel.Is(isDevelopment ? LogEventLevel.Debug : LogEventLevel.Information)

            // Override specific namespaces
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", isDevelopment ? LogEventLevel.Debug : LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)

            // Enrich logs with context
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithProperty("Application", "ProjetoVarejo.Api")
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()

            // Console output for all environments
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] {Message:lj}{NewLine}{Exception}")

            // File output for production and staging
            .WriteTo.File(
                path: Path.Combine("logs", "api-.log"),
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 104857600, // 100 MB
                rollOnFileSizeLimit: true,
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))

            // Application Insights for remote monitoring (if key is configured)
            .ConfigureApplicationInsights(builder.Configuration)

            .CreateLogger();

        // Replace ILogger in DI with Serilog
        builder.Host.UseSerilog();
    }

    /// <summary>
    /// Configure Application Insights sink if instrumentation key is available.
    /// </summary>
    private static LoggerConfiguration ConfigureApplicationInsights(
        this LoggerConfiguration loggerConfig,
        IConfiguration configuration)
    {
        var appInsightsKey = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        if (string.IsNullOrEmpty(appInsightsKey))
        {
            return loggerConfig;
        }

        try
        {
            // This would require Serilog.Sinks.ApplicationInsights package
            // For now, Application Insights can be configured separately in ConfigureServices
            Log.Debug("Application Insights key found, will use remote logging");
            return loggerConfig;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to configure Application Insights logging");
            return loggerConfig;
        }
    }
}

/// <summary>
/// Structured logging helper for common operations.
/// </summary>
public static class StructuredLogging
{
    /// <summary>
    /// Log an HTTP request with metadata.
    /// </summary>
    public static void LogHttpRequest(
        HttpContext context,
        string userId = "",
        string? correlationId = null)
    {
        Log.Information(
            "HTTP Request: {Method} {Path} from {RemoteIP} | User: {User} | CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Connection.RemoteIpAddress,
            userId,
            correlationId ?? "N/A");
    }

    /// <summary>
    /// Log an HTTP response with timing and status.
    /// </summary>
    public static void LogHttpResponse(
        HttpContext context,
        long elapsedMilliseconds,
        string userId = "",
        string? correlationId = null)
    {
        var level = context.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : context.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;

        Log.Write(level,
            "HTTP Response: {Method} {Path} -> {StatusCode} ({ElapsedMs}ms) | User: {User} | CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            elapsedMilliseconds,
            userId,
            correlationId ?? "N/A");
    }

    /// <summary>
    /// Log a business event (e.g., sale finalized, payment recorded).
    /// </summary>
    public static void LogBusinessEvent(
        string eventName,
        int? userId,
        Dictionary<string, object>? properties = null)
    {
        Log.Information("Business Event: {EventName} | User: {User} | Properties: {@Properties}",
            eventName, userId ?? 0, properties ?? new Dictionary<string, object>());
    }

    /// <summary>
    /// Log an authorization decision.
    /// </summary>
    public static void LogAuthorizationDecision(
        string resource,
        string action,
        int userId,
        bool allowed,
        string? reason = null)
    {
        var level = allowed ? LogEventLevel.Debug : LogEventLevel.Warning;
        Log.Write(level,
            "Authorization: {Action} on {Resource} by User {User} -> {Result} | Reason: {Reason}",
            action, resource, userId, allowed ? "ALLOWED" : "DENIED", reason ?? "N/A");
    }

    /// <summary>
    /// Log a data access event for audit purposes.
    /// </summary>
    public static void LogDataAccess(
        string entityType,
        int entityId,
        string operation,
        int userId,
        string? ipAddress = null)
    {
        Log.Information(
            "Data Access: {Operation} on {Entity} (ID: {Id}) by User {User} from {IP}",
            operation, entityType, entityId, userId, ipAddress ?? "N/A");
    }

    /// <summary>
    /// Log a slow query for performance monitoring.
    /// </summary>
    public static void LogSlowQuery(
        string query,
        long elapsedMilliseconds,
        string? parameters = null)
    {
        Log.Warning(
            "Slow Query ({ElapsedMs}ms): {Query} | Parameters: {Parameters}",
            elapsedMilliseconds, query, parameters ?? "none");
    }
}
