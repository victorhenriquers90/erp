namespace ProjetoVarejo.Api.Models;

/// <summary>
/// Standardized generic API response wrapper for all endpoints.
/// Provides consistent success/error handling across the API.
/// </summary>
/// <typeparam name="T">The type of data returned in successful responses</typeparam>
public class ApiResponse<T>
{
    /// <summary>Gets or sets a value indicating whether the operation was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the response data (only populated on success).</summary>
    public T? Data { get; set; }

    /// <summary>Gets or sets the response message.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets the list of detailed error messages (populated on failure).</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>Gets or sets the HTTP error code (only populated on failure).</summary>
    public int? ErrorCode { get; set; }

    /// <summary>Gets or sets the timestamp of the response (UTC).</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful API response.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message ?? "Operação concluída com sucesso"
    };

    /// <summary>
    /// Creates an error API response.
    /// </summary>
    public static ApiResponse<T> Error(string message, int? errorCode = null, List<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        ErrorCode = errorCode ?? 400,
        Errors = errors ?? new()
    };
}

/// <summary>
/// Standardized non-generic API response wrapper for operations that don't return data.
/// </summary>
public class ApiResponse
{
    /// <summary>Gets or sets a value indicating whether the operation was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the response message.</summary>
    public string? Message { get; set; }

    /// <summary>Gets or sets the list of detailed error messages (populated on failure).</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>Gets or sets the HTTP error code (only populated on failure).</summary>
    public int? ErrorCode { get; set; }

    /// <summary>Gets or sets the timestamp of the response (UTC).</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful API response.
    /// </summary>
    public static ApiResponse Ok(string message = "Operação concluída com sucesso") => new()
    {
        Success = true,
        Message = message
    };

    /// <summary>
    /// Creates an error API response.
    /// </summary>
    public static ApiResponse Error(string message, int? errorCode = null, List<string>? errors = null) => new()
    {
        Success = false,
        Message = message,
        ErrorCode = errorCode ?? 400,
        Errors = errors ?? new()
    };
}
