using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Services;

/// <summary>
/// Service abstraction for financial accounts (Contas Financeiras) operations.
/// Handles both payable and receivable accounts with payment tracking.
/// </summary>
public interface IFinanceiroService
{
    /// <summary>
    /// Lists financial accounts with optional filtering by type, status, and date range.
    /// </summary>
    Task<List<ContaFinanceira>> ListarAsync(TipoConta? tipo = null, StatusConta? status = null, DateTime? de = null, DateTime? ate = null);

    /// <summary>
    /// Creates or updates a financial account.
    /// </summary>
    Task<Result<ContaFinanceira>> SalvarAsync(ContaFinanceira c);

    /// <summary>
    /// Marks a financial account as paid/settled with payment details.
    /// </summary>
    Task<Result> QuitarAsync(int contaId, DateTime dataPagamento, decimal valorPago, FormaPagamentoTipo forma, decimal juros = 0, decimal multa = 0, decimal desconto = 0);

    /// <summary>
    /// Gets summary of open and overdue accounts for the given period.
    /// </summary>
    Task<(decimal totalReceber, decimal totalPagar, decimal saldoPrevisto)> ResumoAsync(DateTime de, DateTime ate);

    /// <summary>
    /// Calculates total sales for a specific day.
    /// </summary>
    Task<decimal> TotalVendasDoDiaAsync(DateTime data);
}
