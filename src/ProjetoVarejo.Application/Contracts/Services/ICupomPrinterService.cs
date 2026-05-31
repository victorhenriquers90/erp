using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Services;

/// <summary>
/// Abstraction for printing receipt/cupom on ESC/POS thermal printers.
/// Resides in Application.Contracts to avoid circular dependencies with Infrastructure.
/// </summary>
public interface ICupomPrinterService
{
    /// <summary>
    /// Prints a sale receipt (with or without NFC-e).
    /// Pass <paramref name="nota"/> when the NFC-e was authorized to include fiscal data and QR-code.
    /// </summary>
    Task<Result> ImprimirVendaAsync(Venda venda, EmpresaConfig empresa, NotaFiscal? nota = null);
}
