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

    /// <summary>
    /// Valida credenciais sem alterar a sessão ativa nem atualizar UltimoAcesso.
    /// Usado pelo desbloqueio de supervisor: o operador continua logado enquanto
    /// um administrador/gerente digita a senha para autorizar a ação restrita.
    /// </summary>
    Task<Result<Usuario>> ValidarCredenciaisAsync(string login, string senha);
}
