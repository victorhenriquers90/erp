using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Services;

/// <summary>
/// Service abstraction for NFC-e (Nota Fiscal de Consumidor Eletrônica) operations.
/// Enables dependency injection and testing without Infrastructure layer coupling.
/// </summary>
public interface INfceService
{
    /// <summary>
    /// Verifies if SEFAZ is online and responding.
    /// </summary>
    Task<bool> SefazOnlineAsync();

    /// <summary>
    /// Emits an NFC-e in contingency mode (when SEFAZ is temporarily unavailable).
    /// </summary>
    Task<Result<NotaFiscal>> EmitirContingenciaAsync(int vendaId);

    /// <summary>
    /// Lists all pending NFC-e notes that were emitted in contingency mode.
    /// </summary>
    Task<List<NotaFiscal>> ListarContingenciaPendentesAsync();

    /// <summary>
    /// Resends pending contingency NFC-e to SEFAZ for authorization.
    /// </summary>
    Task<Result<int>> ReenviarContingenciaAsync();

    /// <summary>
    /// Emits an NFC-e for a finalized sale. Sends to SEFAZ for authorization.
    /// </summary>
    Task<Result<NotaFiscal>> EmitirAsync(int vendaId);

    /// <summary>
    /// Gets the currently active company configuration.
    /// </summary>
    Task<EmpresaConfig?> ObterEmpresaAsync();

    /// <summary>
    /// Lists all active company configurations.
    /// </summary>
    Task<List<EmpresaConfig>> ListarEmpresasAsync();

    /// <summary>
    /// Gets a specific company configuration by ID.
    /// </summary>
    Task<EmpresaConfig?> ObterEmpresaPorIdAsync(int id);

    /// <summary>
    /// Creates or updates company configuration.
    /// </summary>
    Task<Result> SalvarEmpresaAsync(EmpresaConfig empresa);

    /// <summary>
    /// Lists NFC-e notes with optional filtering by date range and status.
    /// </summary>
    Task<List<NotaFiscal>> ListarAsync(DateTime? de = null, DateTime? ate = null, StatusNotaFiscal? status = null);

    /// <summary>
    /// Cancels an authorized NFC-e (within 24 hours of authorization).
    /// </summary>
    Task<Result<NotaFiscal>> CancelarAsync(int notaId, string justificativa);

    /// <summary>
    /// Marks a range of sequential NFC-e numbers as inutilized (never to be used).
    /// </summary>
    Task<Result<string>> InutilizarFaixaAsync(int serie, int nNFIni, int nNFFin, string justificativa);
}
