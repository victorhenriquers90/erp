using ProjetoVarejo.Application.Contracts.Services.DTOs;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Services;

/// <summary>
/// Service abstraction for cash register (Caixa) operations.
/// Manages cash sessions, sales registration, and fund movements (withdrawals, supplies).
/// </summary>
public interface ICaixaService
{
    /// <summary>
    /// Retrieves the currently open cash register session for the logged-in user.
    /// </summary>
    Task<CaixaSessao?> ObterCaixaAbertoAsync();

    /// <summary>
    /// Opens a new cash register session with initial float.
    /// </summary>
    Task<Result<CaixaSessao>> AbrirAsync(decimal valorAbertura);

    /// <summary>
    /// Registers a cash withdrawal (sangria) from the open register.
    /// </summary>
    Task<Result<MovimentoCaixa>> SangriaAsync(decimal valor, string motivo);

    /// <summary>
    /// Registers a cash supply (suprimento) to the open register.
    /// </summary>
    Task<Result<MovimentoCaixa>> SuprimentoAsync(decimal valor, string motivo);

    /// <summary>
    /// Records all payments from a completed sale in the cash register.
    /// </summary>
    Task<Result> RegistrarVendaAsync(int caixaId, int vendaId, IEnumerable<PagamentoVenda> pagamentos);

    /// <summary>
    /// Generates a summary of cash register activity and expected balance.
    /// </summary>
    Task<ResumoCaixa> ResumoAsync(int caixaSessaoId);

    /// <summary>
    /// Closes a cash register session with final count reconciliation.
    /// </summary>
    Task<Result<CaixaSessao>> FecharAsync(int caixaSessaoId, decimal valorInformado, string? observacao = null);
}
