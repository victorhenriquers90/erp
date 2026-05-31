using ProjetoVarejo.Api.Extensions;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Contracts.Services.DTOs;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using Serilog;
using Microsoft.AspNetCore.Mvc;

namespace ProjetoVarejo.Api.Endpoints;

/// <summary>
/// API endpoints for cash register (caixa) management.
/// Provides operations for opening/closing sessions, managing cash movements.
/// </summary>
public static class CaixaEndpoints
{
    public static void MapCaixaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/caixa")
            .WithName("Caixa")
            .WithOpenApi()
            .WithTags("Caixa")
            .RequireAuthorization();

        group.MapGet("/current-session", GetCurrentSession)
            .WithName("GetCurrentSession")
            .WithDescription("Obtém a sessão de caixa aberta atualmente");

        group.MapPost("/open-session", OpenSession)
            .WithName("OpenSession")
            .WithDescription("Abre uma nova sessão de caixa")
            .RequireAuthorization("AdminOrGerente");

        group.MapPost("/{id}/close-session", CloseSession)
            .WithName("CloseSession")
            .WithDescription("Fecha a sessão de caixa")
            .RequireAuthorization("AdminOrGerente");

        group.MapPost("/{id}/supply", Supply)
            .WithName("Supply")
            .WithDescription("Adiciona dinheiro ao caixa");

        group.MapPost("/{id}/withdrawal", Withdrawal)
            .WithName("Withdrawal")
            .WithDescription("Remove dinheiro do caixa");

        group.MapGet("/{id}/movements", GetMovements)
            .WithName("GetMovements")
            .WithDescription("Lista os movimentos de caixa");

        group.MapGet("/{id}/summary", GetSummary)
            .WithName("GetSummary")
            .WithDescription("Obtém o resumo do caixa");
    }

    private static async Task<IResult> GetCurrentSession([FromServices] ICaixaService caixaService)
    {
        try
        {
            Log.Information("Obtendo sessão de caixa atual");
            var sessao = await caixaService.ObterCaixaAbertoAsync();
            if (sessao == null)
                return Results.Ok(new ApiResponse { Success = true, Message = "Nenhuma sessão aberta no momento" });
            return Results.Ok(new ApiResponse<CaixaSessao> { Success = true, Data = sessao });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter sessão de caixa");
            return Results.BadRequest(ApiResponse.Error("Erro ao obter sessão: " + ex.Message));
        }
    }

    private static async Task<IResult> OpenSession([FromServices] ICaixaService caixaService, [FromBody] OpenSessionRequest request)
    {
        try
        {
            Log.Information("Abrindo nova sessão de caixa com valor inicial {ValorInicial}", request.ValorInicial);
            var resultado = await caixaService.AbrirAsync(request.ValorInicial);
            return resultado.Sucesso
                ? Results.Created($"/api/caixa/current-session",
                    new ApiResponse<CaixaSessao> { Success = true, Data = resultado.Valor, Message = "Sessão aberta com sucesso" })
                : Results.BadRequest(ApiResponse.Error(resultado.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao abrir sessão de caixa");
            return Results.BadRequest(ApiResponse.Error("Erro ao abrir sessão: " + ex.Message));
        }
    }

    private static async Task<IResult> CloseSession(int id, [FromBody] CloseSessionRequest request, [FromServices] ICaixaService caixaService)
    {
        try
        {
            Log.Information("Fechando sessão de caixa {Id} com valor informado {Valor}", id, request.ValorFechamento);
            var resultado = await caixaService.FecharAsync(id, request.ValorFechamento, request.Observacao);
            return resultado.Sucesso
                ? Results.Ok(new ApiResponse<CaixaSessao> { Success = true, Data = resultado.Valor, Message = "Sessão fechada com sucesso" })
                : Results.BadRequest(ApiResponse.Error(resultado.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao fechar sessão de caixa {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro ao fechar sessão: " + ex.Message));
        }
    }

    private static async Task<IResult> Supply(int id, [FromBody] SupplyRequest request, [FromServices] ICaixaService caixaService)
    {
        try
        {
            Log.Information("Suprimento de {Valor} no caixa {Id}", request.Valor, id);
            var resultado = await caixaService.SuprimentoAsync(request.Valor, request.Observacao ?? "Suprimento via API");
            return resultado.Sucesso
                ? Results.Ok(new ApiResponse<MovimentoCaixa> { Success = true, Data = resultado.Valor, Message = "Suprimento registrado" })
                : Results.BadRequest(ApiResponse.Error(resultado.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao registrar suprimento no caixa");
            return Results.BadRequest(ApiResponse.Error("Erro ao registrar suprimento: " + ex.Message));
        }
    }

    private static async Task<IResult> Withdrawal(int id, [FromBody] WithdrawalRequest request, [FromServices] ICaixaService caixaService)
    {
        try
        {
            Log.Information("Sangria de {Valor} no caixa {Id}", request.Valor, id);
            var resultado = await caixaService.SangriaAsync(request.Valor, request.Observacao ?? "Retirada via API");
            return resultado.Sucesso
                ? Results.Ok(new ApiResponse<MovimentoCaixa> { Success = true, Data = resultado.Valor, Message = "Retirada registrada" })
                : Results.BadRequest(ApiResponse.Error(resultado.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao registrar retirada do caixa");
            return Results.BadRequest(ApiResponse.Error("Erro ao registrar retirada: " + ex.Message));
        }
    }

    private static async Task<IResult> GetMovements(int id, [FromServices] ICaixaService caixaService, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            Log.Information("Obtendo resumo do caixa {Id}", id);
            var resumo = await caixaService.ResumoAsync(id);
            return Results.Ok(new ApiResponse<ResumoCaixa> { Success = true, Data = resumo });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter movimentos do caixa");
            return Results.BadRequest(ApiResponse.Error("Erro ao obter movimentos: " + ex.Message));
        }
    }

    private static async Task<IResult> GetSummary(int id, [FromServices] ICaixaService caixaService)
    {
        try
        {
            Log.Information("Obtendo resumo do caixa {Id}", id);
            var resumo = await caixaService.ResumoAsync(id);
            return Results.Ok(new ApiResponse<ResumoCaixa> { Success = true, Data = resumo });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter resumo do caixa");
            return Results.BadRequest(ApiResponse.Error("Erro ao obter resumo: " + ex.Message));
        }
    }
}

public class OpenSessionRequest
{
    public decimal ValorInicial { get; set; }
}

public class CloseSessionRequest
{
    public decimal ValorFechamento { get; set; }
    public string? Observacao { get; set; }
}

public class SupplyRequest
{
    public decimal Valor { get; set; }
    public string? Observacao { get; set; }
}

public class WithdrawalRequest
{
    public decimal Valor { get; set; }
    public string? Observacao { get; set; }
}
