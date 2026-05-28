using ProjetoVarejo.Api.Models;
using System.Net;
using Serilog;

namespace ProjetoVarejo.Api.Middleware;

/// <summary>
/// Middleware for centralized error handling and response standardization.
/// Catches all exceptions and unhandled status codes, returning consistent ApiResponse format.
/// Logs all errors for monitoring and debugging.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // Handle non-successful status codes that weren't caught by endpoint handlers
            if (context.Response.StatusCode >= 400)
            {
                await HandleErrorResponseAsync(context);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in API request {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleErrorResponseAsync(HttpContext context)
    {
        // Only handle if response body hasn't started
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.ContentType = "application/json";

        var response = context.Response.StatusCode switch
        {
            (int)HttpStatusCode.NotFound => ApiResponse.Error(
                "Recurso não encontrado", 404),
            (int)HttpStatusCode.BadRequest => ApiResponse.Error(
                "Requisição inválida", 400),
            (int)HttpStatusCode.Unauthorized => ApiResponse.Error(
                "Não autorizado. Forneça um token válido.", 401),
            (int)HttpStatusCode.Forbidden => ApiResponse.Error(
                "Acesso proibido. Você não tem permissão para acessar este recurso.", 403),
            _ => ApiResponse.Error(
                "Erro ao processar requisição", context.Response.StatusCode)
        };

        return context.Response.WriteAsJsonAsync(response);
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Only handle if response body hasn't started
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.ContentType = "application/json";

        var (errorCode, response) = exception switch
        {
            ArgumentException => (
                400,
                ApiResponse.Error(exception.Message, 400)
            ),
            UnauthorizedAccessException => (
                401,
                ApiResponse.Error("Não autorizado", 401)
            ),
            InvalidOperationException => (
                400,
                ApiResponse.Error(exception.Message, 400)
            ),
            KeyNotFoundException => (
                404,
                ApiResponse.Error("Recurso não encontrado", 404)
            ),
            _ => (
                500,
                ApiResponse.Error("Erro interno do servidor. Tente novamente mais tarde.", 500)
            )
        };

        context.Response.StatusCode = errorCode;
        return context.Response.WriteAsJsonAsync(response);
    }
}
