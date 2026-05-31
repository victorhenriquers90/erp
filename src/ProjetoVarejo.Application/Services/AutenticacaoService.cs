using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Microsoft.EntityFrameworkCore;

namespace ProjetoVarejo.Application.Services;

public class AutenticacaoService : IAutenticacaoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SessaoApp _sessao;

    public AutenticacaoService(IUnitOfWork unitOfWork, SessaoApp sessao)
    {
        _unitOfWork = unitOfWork;
        _sessao = sessao;
    }

    public async Task<Result<Usuario>> LoginAsync(string login, string senha)
    {
        var usuario = await _unitOfWork.Usuarios.Query().FirstOrDefaultAsync(u => u.Login == login && u.Ativo);
        if (usuario == null) return Result.Falha<Usuario>("Usuário não encontrado.");
        if (!SenhaHasher.Verifica(senha, usuario.SenhaHash))
            return Result.Falha<Usuario>("Senha inválida.");

        usuario.UltimoAcesso = DateTime.Now;
        await _unitOfWork.Usuarios.UpdateAsync(usuario);
        await _unitOfWork.SaveChangesAsync();
        _sessao.DefinirUsuario(usuario);
        return Result.Ok(usuario);
    }

    /// <summary>
    /// Valida credenciais sem alterar a sessão ativa.
    /// Usado pelo desbloqueio de supervisor no PDV.
    /// </summary>
    public async Task<Result<Usuario>> ValidarCredenciaisAsync(string login, string senha)
    {
        var usuario = await _unitOfWork.Usuarios.Query()
            .FirstOrDefaultAsync(u => u.Login == login && u.Ativo);
        if (usuario == null) return Result.Falha<Usuario>("Usuário não encontrado.");
        if (!SenhaHasher.Verifica(senha, usuario.SenhaHash))
            return Result.Falha<Usuario>("Senha inválida.");
        return Result.Ok(usuario);
    }
}
