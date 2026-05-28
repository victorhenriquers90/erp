using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using Serilog;

namespace ProjetoVarejo.Api.Services;

/// <summary>
/// Interface for audit logging of sensitive operations.
/// Tracks authentication, authorization, and data access for compliance.
/// </summary>
public interface IAuditLoggingService
{
    Task LogLoginAsync(int usuarioId, string loginName, bool success, string ipAddress, string? userAgent);
    Task LogDataAccessAsync(int usuarioId, string entityType, int entityId, string operation, string? ipAddress = null);
    Task LogAuthorizationCheckAsync(int usuarioId, string resource, string action, bool allowed, string? reason = null);
    Task LogDataModificationAsync(int usuarioId, string entityType, int entityId, string operation, string? oldValues = null, string? newValues = null);
}

/// <summary>
/// Implementation of audit logging service.
/// Stores audit logs in the database for historical tracking and compliance.
/// </summary>
public class AuditLoggingService : IAuditLoggingService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLoggingService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Log authentication attempts and persist to database.
    /// </summary>
    public async Task LogLoginAsync(int usuarioId, string loginName, bool success, string ipAddress, string? userAgent)
    {
        try
        {
            Log.Information(
                "Login Attempt: {User} ({UserId}) -> {Result} | IP: {IP} | UserAgent: {UserAgent}",
                loginName, usuarioId, success ? "SUCCESS" : "FAILED", ipAddress, userAgent);

            var auditLog = new AuditLog
            {
                UsuarioId = success ? usuarioId : null,
                Entidade = "Autenticacao",
                RegistroId = usuarioId.ToString(),
                Tipo = TipoAuditoria.Insert,
                Data = DateTime.Now,
                ValoresDepois = System.Text.Json.JsonSerializer.Serialize(new
                {
                    Login = loginName,
                    Sucesso = success,
                    IP = ipAddress,
                    UserAgent = userAgent,
                    DataHora = DateTime.UtcNow
                })
            };
            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error logging authentication attempt");
        }
    }

    /// <summary>
    /// Log data access for audit trail.
    /// </summary>
    public async Task LogDataAccessAsync(int usuarioId, string entityType, int entityId, string operation, string? ipAddress = null)
    {
        try
        {
            Log.Information(
                "Data Access: {Operation} on {Entity} ({Id}) by User {User} | IP: {IP}",
                operation, entityType, entityId, usuarioId, ipAddress ?? "Unknown");

            // Data access is logged at debug level only (high-frequency, low-severity)
            // Only persists modifications (handled by LogDataModificationAsync)
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error logging data access");
        }
    }

    /// <summary>
    /// Log authorization checks for security tracking.
    /// </summary>
    public async Task LogAuthorizationCheckAsync(int usuarioId, string resource, string action, bool allowed, string? reason = null)
    {
        try
        {
            var level = allowed ? Serilog.Events.LogEventLevel.Debug : Serilog.Events.LogEventLevel.Warning;
            Log.Write(level,
                "Authorization Check: {Action} on {Resource} by User {User} -> {Result} | Reason: {Reason}",
                action, resource, usuarioId, allowed ? "ALLOWED" : "DENIED", reason ?? "N/A");

            // Persist only denied authorization attempts (security-relevant)
            if (!allowed)
            {
                var auditLog = new AuditLog
                {
                    UsuarioId = usuarioId,
                    Entidade = "Autorizacao",
                    RegistroId = resource,
                    Tipo = TipoAuditoria.Update,
                    Data = DateTime.Now,
                    ValoresDepois = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        Recurso = resource,
                        Acao = action,
                        Resultado = "NEGADO",
                        Motivo = reason
                    })
                };
                _dbContext.AuditLogs.Add(auditLog);
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error logging authorization check");
        }
    }

    /// <summary>
    /// Log data modifications (create/update/delete) for audit trail and persist to database.
    /// </summary>
    public async Task LogDataModificationAsync(
        int usuarioId,
        string entityType,
        int entityId,
        string operation,
        string? oldValues = null,
        string? newValues = null)
    {
        try
        {
            Log.Information(
                "Data Modification: {Operation} on {Entity} ({Id}) by User {User}",
                operation, entityType, entityId, usuarioId);

            var tipo = operation.ToUpperInvariant() switch
            {
                "INSERT" or "CREATE" => TipoAuditoria.Insert,
                "DELETE" => TipoAuditoria.Delete,
                _ => TipoAuditoria.Update
            };

            var auditLog = new AuditLog
            {
                UsuarioId = usuarioId,
                Entidade = entityType,
                RegistroId = entityId.ToString(),
                Tipo = tipo,
                Data = DateTime.Now,
                ValoresAntes = oldValues,
                ValoresDepois = newValues
            };
            _dbContext.AuditLogs.Add(auditLog);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error logging data modification");
        }
    }
}
