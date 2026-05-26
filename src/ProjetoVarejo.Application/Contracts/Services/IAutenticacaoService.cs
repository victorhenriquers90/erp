using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Services;

/// <summary>
/// Service abstraction for authentication (Autenticação) operations.
/// Handles user login and session establishment.
/// </summary>
public interface IAutenticacaoService
{
    /// <summary>
    /// Authenticates a user with login and password.
    /// Updates last access time and establishes the user session.
    /// </summary>
    Task<Result<Usuario>> LoginAsync(string login, string senha);
}
