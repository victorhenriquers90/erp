using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace ProjetoVarejo.Application.Logging;

/// <summary>
/// Configures Serilog logging for the application.
/// Supports multiple sinks: Console, File, and SQL Server.
/// </summary>
public static class LoggingConfiguration
{
    /// <summary>
    /// Configures Serilog with environment-specific settings.
    /// </summary>
    /// <param name="environment">Development, Staging, or Production</param>
    /// <param name="connectionString">SQL Server connection string</param>
    public static void ConfigureSerilog(string environment, string connectionString)
    {
        var minLevel = environment == "Production"
            ? LogEventLevel.Warning
            : LogEventLevel.Information;

        var config = new LoggerConfiguration()
            .MinimumLevel.Is(minLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Filter.ByExcluding(logEvent =>
                logEvent.MessageTemplate.Text.Contains("SELECT", StringComparison.OrdinalIgnoreCase) ||
                logEvent.MessageTemplate.Text.Contains("password", StringComparison.OrdinalIgnoreCase))
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "ProjetoVarejo")
            .Enrich.WithProperty("Environment", environment);

        // Console output (always, helpful for debugging)
        config.WriteTo.Console(
            outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

        // File output
        config.WriteTo.File(
            path: "logs/projetovarejo-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

        // SQL Server output (if connection string is provided)
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            try
            {
                config.WriteTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = "Logs",
                        AutoCreateSqlTable = true,
                        BatchPostingLimit = 100,
                        BatchPeriod = TimeSpan.FromSeconds(1)
                    });
            }
            catch
            {
                // If SQL Server sink fails, continue with console and file only
                // This allows the app to start even if the database is unavailable
            }
        }

        Log.Logger = config.CreateLogger();
    }

    /// <summary>
    /// Closes and flushes Serilog (should be called on application shutdown).
    /// </summary>
    public static void FlushAndClose()
    {
        Log.CloseAndFlush();
    }
}
