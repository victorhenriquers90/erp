using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Services.DTOs;

/// <summary>
/// DTO representing a summary of cash register activity for a session.
/// </summary>
public class ResumoCaixa
{
    /// <summary>
    /// Initial float value when the register was opened.
    /// </summary>
    public decimal ValorAbertura { get; set; }

    /// <summary>
    /// Total cash supplies added during the session.
    /// </summary>
    public decimal TotalSuprimentos { get; set; }

    /// <summary>
    /// Total cash withdrawals during the session.
    /// </summary>
    public decimal TotalSangrias { get; set; }

    /// <summary>
    /// Sales broken down by payment method.
    /// </summary>
    public Dictionary<FormaPagamentoTipo, decimal> VendasPorForma { get; set; } = new();

    /// <summary>
    /// Total sales across all payment methods.
    /// </summary>
    public decimal TotalVendas => VendasPorForma.Values.Sum();

    /// <summary>
    /// Expected cash balance calculated from opening value, movements, and sales.
    /// </summary>
    public decimal SaldoDinheiroEsperado =>
        ValorAbertura
        + TotalSuprimentos
        - TotalSangrias
        + (VendasPorForma.TryGetValue(FormaPagamentoTipo.Dinheiro, out var d) ? d : 0);
}
