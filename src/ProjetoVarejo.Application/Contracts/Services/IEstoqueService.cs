using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Services;

/// <summary>
/// Service abstraction for inventory (Estoque) operations.
/// Handles stock movements, adjustments, and tracking.
/// </summary>
public interface IEstoqueService
{
    /// <summary>
    /// Registers a stock movement (entry, exit, adjustment, return, etc.).
    /// Validates quantities, updates product stock, and handles concurrency conflicts.
    /// </summary>
    Task<Result<MovimentoEstoque>> RegistrarMovimentoAsync(
        int produtoId,
        TipoMovimentoEstoque tipo,
        decimal quantidade,
        decimal? custoUnitario = null,
        string? documento = null,
        int? vendaId = null,
        int? fornecedorId = null,
        string? observacao = null);

    /// <summary>
    /// Lists stock movements with optional filtering by product, date range.
    /// </summary>
    Task<List<MovimentoEstoque>> ListarMovimentosAsync(int? produtoId = null, DateTime? de = null, DateTime? ate = null);

    /// <summary>
    /// Lists products with stock below minimum threshold.
    /// </summary>
    Task<List<Produto>> ProdutosAbaixoMinimoAsync();
}
