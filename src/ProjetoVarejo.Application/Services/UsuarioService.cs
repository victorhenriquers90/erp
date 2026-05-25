using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class UsuarioService
{
    private readonly AppDbContext _db;

    public UsuarioService(AppDbContext db) => _db = db;

    public Task<List<Usuario>> ListarAsync(string? filtro = null, bool incluirInativos = true)
    {
        var q = _db.Usuarios.AsQueryable();

        if (!incluirInativos)
            q = q.Where(u => u.Ativo);

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            var termo = filtro.Trim();
            q = q.Where(u => u.Nome.Contains(termo) || u.Login.Contains(termo));
        }

        return q.OrderByDescending(u => u.Ativo)
            .ThenBy(u => u.Nome)
            .ToListAsync();
    }

    public Task<Usuario?> BuscarPorIdAsync(int id) =>
        _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<Result<Usuario>> SalvarAsync(Usuario usuario, string? senha = null)
    {
        usuario.Login = NormalizarLogin(usuario.Login);
        usuario.Nome = usuario.Nome.Trim();

        if (string.IsNullOrWhiteSpace(usuario.Login))
            return Result.Falha<Usuario>("Login e obrigatorio.");
        if (string.IsNullOrWhiteSpace(usuario.Nome))
            return Result.Falha<Usuario>("Nome e obrigatorio.");

        var duplicado = await _db.Usuarios.AnyAsync(u => u.Login == usuario.Login && u.Id != usuario.Id);
        if (duplicado)
            return Result.Falha<Usuario>("Ja existe usuario com este login.");

        if (!await MantemAdministradorAtivoAsync(usuario))
            return Result.Falha<Usuario>("E necessario manter ao menos um administrador ativo.");

        if (usuario.Id == 0)
        {
            var validacaoSenha = ValidarSenha(senha, obrigatoria: true);
            if (!validacaoSenha.Sucesso) return Result.Falha<Usuario>(validacaoSenha.Erro!);

            usuario.SenhaHash = SenhaHasher.Hash(senha!);
            _db.Usuarios.Add(usuario);
        }
        else
        {
            var atual = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuario.Id);
            if (atual == null) return Result.Falha<Usuario>("Usuario nao encontrado.");

            atual.Login = usuario.Login;
            atual.Nome = usuario.Nome;
            atual.Perfil = usuario.Perfil;
            atual.Ativo = usuario.Ativo;
            atual.AtualizadoEm = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(senha))
            {
                var validacaoSenha = ValidarSenha(senha, obrigatoria: false);
                if (!validacaoSenha.Sucesso) return Result.Falha<Usuario>(validacaoSenha.Erro!);
                atual.SenhaHash = SenhaHasher.Hash(senha);
            }

            usuario = atual;
        }

        await _db.SaveChangesAsync();
        return Result.Ok(usuario);
    }

    public async Task<Result> RedefinirSenhaAsync(int usuarioId, string novaSenha)
    {
        var validacaoSenha = ValidarSenha(novaSenha, obrigatoria: true);
        if (!validacaoSenha.Sucesso) return validacaoSenha;

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario == null) return Result.Falha("Usuario nao encontrado.");

        usuario.SenhaHash = SenhaHasher.Hash(novaSenha);
        usuario.AtualizadoEm = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> AlternarAtivoAsync(int usuarioId)
    {
        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario == null) return Result.Falha("Usuario nao encontrado.");

        var copia = new Usuario
        {
            Id = usuario.Id,
            Login = usuario.Login,
            Nome = usuario.Nome,
            Perfil = usuario.Perfil,
            Ativo = !usuario.Ativo
        };

        if (!await MantemAdministradorAtivoAsync(copia))
            return Result.Falha("E necessario manter ao menos um administrador ativo.");

        usuario.Ativo = !usuario.Ativo;
        usuario.AtualizadoEm = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result.Ok();
    }

    private async Task<bool> MantemAdministradorAtivoAsync(Usuario usuario)
    {
        if (usuario.Ativo && usuario.Perfil == PerfilUsuario.Administrador)
            return true;

        return await _db.Usuarios.AnyAsync(u =>
            u.Id != usuario.Id
            && u.Ativo
            && u.Perfil == PerfilUsuario.Administrador);
    }

    private static Result ValidarSenha(string? senha, bool obrigatoria)
    {
        if (string.IsNullOrWhiteSpace(senha))
            return obrigatoria ? Result.Falha("Senha e obrigatoria.") : Result.Ok();
        if (senha.Length < 6)
            return Result.Falha("Senha deve ter pelo menos 6 caracteres.");
        return Result.Ok();
    }

    private static string NormalizarLogin(string login) =>
        login.Trim().ToLowerInvariant();
}
