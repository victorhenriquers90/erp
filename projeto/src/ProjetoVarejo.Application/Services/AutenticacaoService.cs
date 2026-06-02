using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class AutenticacaoService
{
    private readonly AppDbContext _db;
    private readonly SessaoApp _sessao;

    public AutenticacaoService(AppDbContext db, SessaoApp sessao)
    {
        _db = db;
        _sessao = sessao;
    }

    public async Task<Result<Usuario>> LoginAsync(string login, string senha)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Login == login && u.Ativo);
        if (usuario == null) return Result.Falha<Usuario>("Usuário não encontrado.");
        if (!SenhaHasher.Verifica(senha, usuario.SenhaHash))
            return Result.Falha<Usuario>("Senha inválida.");

        usuario.UltimoAcesso = DateTime.Now;
        await _db.SaveChangesAsync();
        _sessao.DefinirUsuario(usuario);
        return Result.Ok(usuario);
    }
}
