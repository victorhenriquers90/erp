using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Api.Extensions;

/// <summary>
/// Extension methods for standardizing responses from service layer to API responses.
/// Converts Result<T> and business logic responses to ApiResponse<T> for consistent error handling.
/// </summary>
public static class ResponseExtensions
{
    /// <summary>
    /// Convert Result<T> from service layer to IResult for API response.
    /// Handles both success and failure cases with proper HTTP status codes.
    /// </summary>
    /// <typeparam name="T">The type of data in the result</typeparam>
    /// <param name="result">The Result<T> from service layer</param>
    /// <returns>IResult with proper ApiResponse format</returns>
    public static IResult ToApiResponse<T>(this Result<T> result)
    {
        if (result.Sucesso)
        {
            return Results.Ok(new ApiResponse<T>
            {
                Success = true,
                Data = result.Valor!,
                Message = "Operação concluída com sucesso"
            });
        }

        return Results.BadRequest(new ApiResponse<T>
        {
            Success = false,
            Message = result.Erro ?? "Operação falhou",
            Errors = new List<string> { result.Erro ?? "Operação falhou" },
            ErrorCode = 400
        });
    }

    /// <summary>
    /// Convert Result (non-generic) from service layer to IResult for API response.
    /// </summary>
    /// <param name="result">The Result from service layer</param>
    /// <returns>IResult with proper ApiResponse format</returns>
    public static IResult ToApiResponse(this Result result)
    {
        if (result.Sucesso)
        {
            return Results.Ok(new ApiResponse
            {
                Success = true,
                Message = "Operação concluída com sucesso"
            });
        }

        return Results.BadRequest(new ApiResponse
        {
            Success = false,
            Message = result.Erro ?? "Operação falhou",
            Errors = new List<string> { result.Erro ?? "Operação falhou" },
            ErrorCode = 400
        });
    }

    /// <summary>
    /// Wrap successful data in ApiResponse.
    /// </summary>
    /// <typeparam name="T">The type of data to wrap</typeparam>
    /// <param name="data">The data to wrap</param>
    /// <param name="message">Optional custom success message</param>
    /// <returns>ApiResponse<T> in success state</returns>
    public static ApiResponse<T> WrapSuccess<T>(this T data, string? message = null) =>
        ApiResponse<T>.Ok(data, message);

    /// <summary>
    /// Create error ApiResponse.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="errorCode">Optional HTTP error code (default 500)</param>
    /// <param name="errors">Optional list of detailed error messages</param>
    /// <returns>ApiResponse in error state</returns>
    public static ApiResponse Error(string message, int? errorCode = 500, List<string>? errors = null) =>
        ApiResponse.Error(message, errorCode, errors);
}
