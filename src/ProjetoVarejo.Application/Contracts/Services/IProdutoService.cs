using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Services;

/// <summary>
/// Service abstraction for product (Produto) operations.
/// Handles product search, creation, modification, and deactivation.
/// </summary>
public interface IProdutoService
{
    /// <summary>
    /// Lists all products with optional filter by code, description, or barcode.
    /// </summary>
    Task<List<Produto>> ListarAsync(string? filtro = null);

    /// <summary>
    /// Lists only active products suitable for sale with optional filter.
    /// </summary>
    Task<List<Produto>> ListarParaVendaAsync(string? filtro = null);

    /// <summary>
    /// Finds an active product by code or barcode.
    /// </summary>
    Task<Produto?> BuscarPorCodigoAsync(string codigo);

    /// <summary>
    /// Retrieves a product by ID with its category information.
    /// </summary>
    Task<Produto?> BuscarPorIdAsync(int id);

    /// <summary>
    /// Creates or updates a product. Validates uniqueness of product code.
    /// </summary>
    Task<Result<Produto>> SalvarAsync(Produto produto);

    /// <summary>
    /// Deletes a product (or marks inactive if it has sales history).
    /// </summary>
    Task<Result> ExcluirAsync(int id);
}
