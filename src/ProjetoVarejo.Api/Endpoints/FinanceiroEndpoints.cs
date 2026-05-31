using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Api.Extensions;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Serilog;
using Microsoft.AspNetCore.Mvc;

namespace ProjetoVarejo.Api.Endpoints;

/// <summary>
/// API endpoints for financial management (Contas a Pagar e Receber).
/// </summary>
public static class FinanceiroEndpoints
{
    public static void MapFinanceiroEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/financeiro")
            .WithName("Financeiro")
            .WithOpenApi()
            .WithTags("Financeiro")
            .RequireAuthorization();

        // Contas financeiras (A Pagar / A Receber)
        group.MapGet("/contas", ListarContas).WithName("ListarContas");
        group.MapGet("/contas/{id}", ObterConta).WithName("ObterConta");
        group.MapPost("/contas", CriarConta)
            .WithName("CriarConta")
            .RequireAuthorization("CanViewFinancials");
        group.MapPut("/contas/{id}", AtualizarConta)
            .WithName("AtualizarConta")
            .RequireAuthorization("CanViewFinancials");
        group.MapDelete("/contas/{id}", CancelarConta)
            .WithName("CancelarConta")
            .RequireAuthorization("AdminOnly");
        group.MapPost("/contas/{id}/quitar", QuitarConta)
            .WithName("QuitarConta")
            .RequireAuthorization("CanViewFinancials");

        // Lançamentos (alias filtrado de contas com paginação)
        group.MapGet("/lancamentos", ListarLancamentos).WithName("ListarLancamentos");
        group.MapPost("/lancamentos", CriarLancamento)
            .WithName("CriarLancamento")
            .RequireAuthorization("CanViewFinancials");
        group.MapPut("/lancamentos/{id}", AtualizarLancamento)
            .WithName("AtualizarLancamento")
            .RequireAuthorization("CanViewFinancials");
        group.MapDelete("/lancamentos/{id}", DeletarLancamento)
            .WithName("DeletarLancamento")
            .RequireAuthorization("AdminOnly");
        group.MapPost("/lancamentos/{id}/marcar-pago", MarcarPago)
            .WithName("MarcarPago")
            .RequireAuthorization("CanViewFinancials");

        // Relatórios
        group.MapGet("/relatorio", GetRelatorio).WithName("GetRelatorio");
        group.MapGet("/resumo", GetResumo).WithName("GetResumoFinanceiro");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Contas
    // ──────────────────────────────────────────────────────────────────────────

    private static async Task<IResult> ListarContas(
        [FromServices] IFinanceiroService svc,
        [FromQuery] TipoConta? tipo = null,
        [FromQuery] StatusConta? status = null,
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            Log.Information("Listando contas financeiras. tipo={Tipo} status={Status}", tipo, status);
            var lista = await svc.ListarAsync(tipo, status, de, ate);
            var paginado = lista.Paginate(page, pageSize);
            return Results.Ok(new ApiResponse<PagedResult<ContaFinanceira>>
            {
                Success = true,
                Data = paginado,
                Message = $"{paginado.TotalCount} conta(s) encontrada(s)"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao listar contas financeiras");
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> ObterConta(int id, [FromServices] IUnitOfWork uow)
    {
        try
        {
            Log.Information("Obtendo conta financeira {Id}", id);
            var conta = await uow.ContasFinanceiras.GetByIdAsync(id);
            if (conta == null)
                return Results.NotFound(ApiResponse.Error($"Conta {id} não encontrada", 404));
            return Results.Ok(new ApiResponse<ContaFinanceira> { Success = true, Data = conta });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter conta {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> CriarConta(
        [FromBody] ContaFinanceiraRequest req,
        [FromServices] IFinanceiroService svc)
    {
        try
        {
            Log.Information("Criando conta financeira: {Descricao}", req.Descricao);
            var conta = req.ToEntity();
            var res = await svc.SalvarAsync(conta);
            return res.Sucesso
                ? Results.Created($"/api/financeiro/contas/{res.Valor!.Id}",
                    new ApiResponse<ContaFinanceira> { Success = true, Data = res.Valor, Message = "Conta criada" })
                : Results.BadRequest(ApiResponse.Error(res.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao criar conta financeira");
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> AtualizarConta(
        int id,
        [FromBody] ContaFinanceiraRequest req,
        [FromServices] IFinanceiroService svc,
        [FromServices] IUnitOfWork uow)
    {
        try
        {
            Log.Information("Atualizando conta financeira {Id}", id);
            var conta = await uow.ContasFinanceiras.GetByIdAsync(id);
            if (conta == null)
                return Results.NotFound(ApiResponse.Error($"Conta {id} não encontrada", 404));

            req.ApplyTo(conta);
            var res = await svc.SalvarAsync(conta);
            return res.Sucesso
                ? Results.Ok(new ApiResponse<ContaFinanceira> { Success = true, Data = res.Valor, Message = "Conta atualizada" })
                : Results.BadRequest(ApiResponse.Error(res.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao atualizar conta {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> CancelarConta(int id, [FromServices] IUnitOfWork uow)
    {
        try
        {
            Log.Information("Cancelando conta financeira {Id}", id);
            var conta = await uow.ContasFinanceiras.GetByIdAsync(id);
            if (conta == null)
                return Results.NotFound(ApiResponse.Error($"Conta {id} não encontrada", 404));
            if (conta.Status == StatusConta.Paga)
                return Results.BadRequest(ApiResponse.Error("Conta já paga não pode ser cancelada."));

            conta.Status = StatusConta.Cancelada;
            await uow.ContasFinanceiras.UpdateAsync(conta);
            await uow.SaveChangesAsync();
            return Results.Ok(ApiResponse.Ok("Conta cancelada"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao cancelar conta {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> QuitarConta(
        int id,
        [FromBody] QuitarContaRequest req,
        [FromServices] IFinanceiroService svc)
    {
        try
        {
            Log.Information("Quitando conta {Id}", id);
            var res = await svc.QuitarAsync(id, req.DataPagamento ?? DateTime.Today,
                req.ValorPago, req.FormaPagamento, req.Juros, req.Multa, req.Desconto);
            return res.Sucesso
                ? Results.Ok(ApiResponse.Ok("Conta quitada com sucesso"))
                : Results.BadRequest(ApiResponse.Error(res.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao quitar conta {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Lançamentos (same entity, legacy naming kept for API compatibility)
    // ──────────────────────────────────────────────────────────────────────────

    private static async Task<IResult> ListarLancamentos(
        [FromServices] IFinanceiroService svc,
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            Log.Information("Listando lançamentos financeiros");
            var lista = await svc.ListarAsync(null, null, de, ate);
            var paginado = lista.Paginate(page, pageSize);
            return Results.Ok(new ApiResponse<PagedResult<ContaFinanceira>>
            {
                Success = true,
                Data = paginado,
                Message = $"{paginado.TotalCount} lançamento(s)"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao listar lançamentos");
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> CriarLancamento(
        [FromBody] ContaFinanceiraRequest req,
        [FromServices] IFinanceiroService svc)
    {
        try
        {
            Log.Information("Criando lançamento: {Descricao}", req.Descricao);
            var conta = req.ToEntity();
            var res = await svc.SalvarAsync(conta);
            return res.Sucesso
                ? Results.Created($"/api/financeiro/lancamentos/{res.Valor!.Id}",
                    new ApiResponse<ContaFinanceira> { Success = true, Data = res.Valor, Message = "Lançamento criado" })
                : Results.BadRequest(ApiResponse.Error(res.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao criar lançamento");
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> AtualizarLancamento(
        int id,
        [FromBody] ContaFinanceiraRequest req,
        [FromServices] IFinanceiroService svc,
        [FromServices] IUnitOfWork uow)
    {
        try
        {
            Log.Information("Atualizando lançamento {Id}", id);
            var conta = await uow.ContasFinanceiras.GetByIdAsync(id);
            if (conta == null)
                return Results.NotFound(ApiResponse.Error($"Lançamento {id} não encontrado", 404));

            req.ApplyTo(conta);
            var res = await svc.SalvarAsync(conta);
            return res.Sucesso
                ? Results.Ok(new ApiResponse<ContaFinanceira> { Success = true, Data = res.Valor, Message = "Lançamento atualizado" })
                : Results.BadRequest(ApiResponse.Error(res.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao atualizar lançamento {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> DeletarLancamento(int id, [FromServices] IUnitOfWork uow)
    {
        try
        {
            Log.Information("Cancelando lançamento {Id}", id);
            var conta = await uow.ContasFinanceiras.GetByIdAsync(id);
            if (conta == null)
                return Results.NotFound(ApiResponse.Error($"Lançamento {id} não encontrado", 404));
            if (conta.Status == StatusConta.Paga)
                return Results.BadRequest(ApiResponse.Error("Lançamento já pago não pode ser removido."));

            conta.Status = StatusConta.Cancelada;
            await uow.ContasFinanceiras.UpdateAsync(conta);
            await uow.SaveChangesAsync();
            return Results.Ok(ApiResponse.Ok("Lançamento cancelado"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao cancelar lançamento {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> MarcarPago(
        int id,
        [FromBody] QuitarContaRequest req,
        [FromServices] IFinanceiroService svc)
    {
        try
        {
            Log.Information("Marcando lançamento {Id} como pago", id);
            var res = await svc.QuitarAsync(id, req.DataPagamento ?? DateTime.Today,
                req.ValorPago, req.FormaPagamento, req.Juros, req.Multa, req.Desconto);
            return res.Sucesso
                ? Results.Ok(ApiResponse.Ok("Lançamento marcado como pago"))
                : Results.BadRequest(ApiResponse.Error(res.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao marcar como pago {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Relatórios
    // ──────────────────────────────────────────────────────────────────────────

    private static async Task<IResult> GetRelatorio(
        [FromServices] IFinanceiroService svc,
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null)
    {
        try
        {
            Log.Information("Gerando relatório financeiro");
            var inicio = de ?? DateTime.Today.AddDays(-30);
            var fim = ate ?? DateTime.Today.AddDays(1);
            var (totalReceber, totalPagar, saldoPrevisto) = await svc.ResumoAsync(inicio, fim);
            return Results.Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    Periodo = new { De = inicio, Ate = fim },
                    TotalReceber = totalReceber,
                    TotalPagar = totalPagar,
                    SaldoPrevisto = saldoPrevisto
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao gerar relatório financeiro");
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> GetResumo(
        [FromServices] IFinanceiroService svc,
        [FromQuery] DateTime? de = null,
        [FromQuery] DateTime? ate = null)
    {
        try
        {
            Log.Information("Gerando resumo financeiro");
            var inicio = de ?? DateTime.Today;
            var fim = ate ?? DateTime.Today.AddDays(1);
            var (totalReceber, totalPagar, saldoPrevisto) = await svc.ResumoAsync(inicio, fim);
            var vendasHoje = await svc.TotalVendasDoDiaAsync(DateTime.Today);
            return Results.Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    TotalReceber = totalReceber,
                    TotalPagar = totalPagar,
                    SaldoPrevisto = saldoPrevisto,
                    VendasHoje = vendasHoje
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao gerar resumo financeiro");
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Request models
// ──────────────────────────────────────────────────────────────────────────────

public class ContaFinanceiraRequest
{
    public TipoConta Tipo { get; set; } = TipoConta.Pagar;
    public string Descricao { get; set; } = string.Empty;
    public string? DocumentoNumero { get; set; }
    public DateTime DataEmissao { get; set; } = DateTime.Today;
    public DateTime DataVencimento { get; set; }
    public decimal Valor { get; set; }
    public int? ClienteId { get; set; }
    public int? FornecedorId { get; set; }
    public string? Observacao { get; set; }

    public ContaFinanceira ToEntity() => new()
    {
        Tipo = Tipo,
        Descricao = Descricao,
        DocumentoNumero = DocumentoNumero,
        DataEmissao = DataEmissao,
        DataVencimento = DataVencimento,
        Valor = Valor,
        ClienteId = ClienteId,
        FornecedorId = FornecedorId,
        Observacao = Observacao,
        Status = StatusConta.EmAberto
    };

    public void ApplyTo(ContaFinanceira c)
    {
        c.Tipo = Tipo;
        c.Descricao = Descricao;
        c.DocumentoNumero = DocumentoNumero;
        c.DataEmissao = DataEmissao;
        c.DataVencimento = DataVencimento;
        c.Valor = Valor;
        c.ClienteId = ClienteId;
        c.FornecedorId = FornecedorId;
        c.Observacao = Observacao;
    }
}

public class QuitarContaRequest
{
    public DateTime? DataPagamento { get; set; }
    public decimal ValorPago { get; set; }
    public FormaPagamentoTipo FormaPagamento { get; set; } = FormaPagamentoTipo.Dinheiro;
    public decimal Juros { get; set; }
    public decimal Multa { get; set; }
    public decimal Desconto { get; set; }
}

// Legacy compat — kept for any existing clients
public class CreateContaRequest { public string Nome { get; set; } = string.Empty; }
public class UpdateContaRequest { public string Nome { get; set; } = string.Empty; }
public class CreateLancamentoRequest { public string Descricao { get; set; } = string.Empty; public decimal Valor { get; set; } }
public class UpdateLancamentoRequest { public string Descricao { get; set; } = string.Empty; public decimal Valor { get; set; } }
