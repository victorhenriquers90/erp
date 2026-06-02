using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Api.Endpoints;

public static class FinanceiroEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/api/financeiro").WithTags("Financeiro");

        g.MapGet("/contas", async (FinanceiroService svc, TipoConta? tipo, StatusConta? status, DateTime? de, DateTime? ate) =>
        {
            var lista = await svc.ListarAsync(tipo, status, de, ate);
            return Results.Ok(lista.Select(c => new
            {
                c.Id,
                Tipo = c.Tipo.ToString(),
                c.Descricao, c.DocumentoNumero,
                c.DataEmissao, c.DataVencimento, c.DataPagamento,
                c.Valor, c.ValorPago,
                Status = c.Status.ToString(),
                FormaPagamento = c.FormaPagamento.HasValue ? c.FormaPagamento.ToString() : null,
                Cliente = c.Cliente?.Nome,
                Fornecedor = c.Fornecedor?.RazaoSocial,
                c.Observacao
            }));
        });

        g.MapPost("/contas", async (ContaFinanceira conta, FinanceiroService svc) =>
        {
            var result = await svc.SalvarAsync(conta);
            if (!result.Sucesso) return Results.BadRequest(new { erro = result.Erro });
            return Results.Created($"/api/financeiro/contas/{result.Valor!.Id}", new { result.Valor!.Id });
        });

        g.MapPut("/contas/{id:int}/quitar", async (int id, QuitarRequest req, FinanceiroService svc) =>
        {
            var result = await svc.QuitarAsync(id, req.DataPagamento, req.ValorPago, req.Forma, req.Juros, req.Multa, req.Desconto);
            if (!result.Sucesso) return Results.BadRequest(new { erro = result.Erro });
            return Results.Ok(new { ok = true });
        });

        g.MapGet("/resumo", async (FinanceiroService svc, DateTime de, DateTime ate) =>
        {
            var (totalReceber, totalPagar, saldoPrevisto) = await svc.ResumoAsync(de, ate);
            return Results.Ok(new { totalReceber, totalPagar, saldoPrevisto, de, ate });
        });
    }

    public record QuitarRequest(
        decimal ValorPago,
        DateTime DataPagamento,
        FormaPagamentoTipo Forma,
        decimal Juros = 0,
        decimal Multa = 0,
        decimal Desconto = 0);
}
