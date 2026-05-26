using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Services;

/// <summary>
/// Service abstraction for sales (Venda) operations.
/// Handles creation, modification, finalization, and cancellation of sales.
/// </summary>
public interface IVendaService
{
    /// <summary>
    /// Creates a new open sale with a unique sequential number.
    /// </summary>
    Task<Result<Venda>> NovaVendaAsync();

    /// <summary>
    /// Adds a product item to an open sale.
    /// </summary>
    Task<Result<ItemVenda>> AdicionarItemAsync(int vendaId, int produtoId, decimal quantidade, decimal? precoOverride = null);

    /// <summary>
    /// Removes an item from an open sale.
    /// </summary>
    Task<Result> RemoverItemAsync(int itemId);

    /// <summary>
    /// Recalculates subtotal and total for a sale based on its items.
    /// </summary>
    Task RecalcularTotaisAsync(int vendaId);

    /// <summary>
    /// Applies a discount to an open sale.
    /// </summary>
    Task<Result> AplicarDescontoAsync(int vendaId, decimal desconto);

    /// <summary>
    /// Finalizes a sale with payment information. Deducts stock for items.
    /// </summary>
    Task<Result<Venda>> FinalizarAsync(int vendaId, List<PagamentoVenda> pagamentos, int? clienteId = null);

    /// <summary>
    /// Cancels an existing sale and returns stock if finalized.
    /// </summary>
    Task<Result> CancelarAsync(int vendaId, string motivo);

    /// <summary>
    /// Retrieves a complete sale with all related data (items, payments, customer).
    /// </summary>
    Task<Venda?> BuscarAsync(int id);

    /// <summary>
    /// Lists sales with optional filtering by date range and status.
    /// </summary>
    Task<List<Venda>> ListarAsync(DateTime? de = null, DateTime? ate = null, StatusVenda? status = null);
}
