using Serilog.Context;

namespace ProjetoVarejo.Application.Logging;

/// <summary>
/// Manages structured logging context with correlation IDs and user information.
/// Automatically adds properties to all log entries within the scope.
/// </summary>
public class StructuredLogContext : IDisposable
{
    private readonly IDisposable? _logContext;

    /// <summary>
    /// Initializes a new structured logging context with correlation tracking.
    /// </summary>
    /// <param name="correlationId">Unique identifier to correlate related operations</param>
    /// <param name="userId">ID of the user performing the operation</param>
    /// <param name="operation">Name of the operation being performed</param>
    public StructuredLogContext(string correlationId, string userId, string operation)
    {
        var timestamp = DateTime.UtcNow;

        // Push each property individually using PushProperty
        LogContext.PushProperty("CorrelationId", correlationId);
        LogContext.PushProperty("UserId", userId);
        LogContext.PushProperty("Operation", operation);
        _logContext = LogContext.PushProperty("ContextTimestamp", timestamp);
    }

    /// <summary>
    /// Initializes a new structured logging context without user information.
    /// </summary>
    /// <param name="correlationId">Unique identifier to correlate related operations</param>
    /// <param name="operation">Name of the operation being performed</param>
    public StructuredLogContext(string correlationId, string operation)
        : this(correlationId, "Sistema", operation)
    {
    }

    /// <summary>
    /// Initializes a new structured logging context with a generated correlation ID.
    /// </summary>
    /// <param name="operation">Name of the operation being performed</param>
    public StructuredLogContext(string operation)
        : this(Guid.NewGuid().ToString(), "Sistema", operation)
    {
    }

    /// <summary>
    /// Disposes the log context, removing all pushed properties.
    /// </summary>
    public void Dispose()
    {
        _logContext?.Dispose();
    }
}
