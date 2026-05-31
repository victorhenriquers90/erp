using ProjetoVarejo.Application.Contracts.Services.DTOs;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Services;

/// <summary>
/// Service abstraction for reporting (Relatório) operations.
/// Provides various business intelligence and analytics queries.
/// </summary>
public interface IRelatorioService
{
    /// <summary>
    /// Daily sales report showing quantity and total per day.
    /// </summary>
    Task<List<VendaDiariaItem>> VendasPorDiaAsync(DateTime de, DateTime ate);

    /// <summary>
    /// Sales by payment method for the given period.
    /// </summary>
    Task<List<VendaPorFormaItem>> VendasPorFormaPagamentoAsync(DateTime de, DateTime ate);

    /// <summary>
    /// Sales by salesperson with average ticket calculation.
    /// </summary>
    Task<List<VendaPorVendedorItem>> VendasPorVendedorAsync(DateTime de, DateTime ate);

    /// <summary>
    /// ABC Curve analysis of products sold (80-20 rule classification).
    /// </summary>
    Task<List<ProdutoRankingItem>> CurvaAbcAsync(DateTime de, DateTime ate);

    /// <summary>
    /// Top N best-selling products by revenue.
    /// </summary>
    Task<List<ProdutoRankingItem>> TopProdutosAsync(DateTime de, DateTime ate, int n = 20);

    /// <summary>
    /// Cash flow analysis showing daily inflows, outflows, and balance.
    /// </summary>
    Task<List<FluxoCaixaItem>> FluxoCaixaAsync(DateTime de, DateTime ate);
}
