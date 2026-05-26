using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Logging;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Serilog;

namespace ProjetoVarejo.Application.Services;

public class UsuarioService
{
    private readonly IUnitOfWork _unitOfWork;

    public UsuarioService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public Task<List<Usuario>> ListarAsync(string? filtro = null, bool incluirInativos = true)
    {
        var q = _unitOfWork.Usuarios.Query().AsQueryable();

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
        _unitOfWork.Usuarios.Query().FirstOrDefaultAsync(u => u.Id == id);

    public async Task<Result<Usuario>> SalvarAsync(Usuario usuario, string? senha = null)
    {
        try
        {
            usuario.Login = NormalizarLogin(usuario.Login);
            usuario.Nome = usuario.Nome.Trim();

            if (string.IsNullOrWhiteSpace(usuario.Login))
            {
                Log.Warning("Tentativa de salvar usuário sem login");
                return Result.Falha<Usuario>("Login e obrigatorio.");
            }

            if (string.IsNullOrWhiteSpace(usuario.Nome))
            {
                Log.Warning("Tentativa de salvar usuário sem nome");
                return Result.Falha<Usuario>("Nome e obrigatorio.");
            }

            var duplicado = await _unitOfWork.Usuarios.Query().AnyAsync(u => u.Login == usuario.Login && u.Id != usuario.Id);
            if (duplicado)
            {
                Log.Warning("Tentativa de criar usuário com login duplicado: {Login}", usuario.Login);
                return Result.Falha<Usuario>("Ja existe usuario com este login.");
            }

            if (!await MantemAdministradorAtivoAsync(usuario))
            {
                Log.Warning("Tentativa de desativar único administrador");
                return Result.Falha<Usuario>("E necessario manter ao menos um administrador ativo.");
            }

            if (usuario.Id == 0)
            {
                var validacaoSenha = ValidarSenha(senha, obrigatoria: true);
                if (!validacaoSenha.Sucesso) return Result.Falha<Usuario>(validacaoSenha.Erro!);

                usuario.SenhaHash = SenhaHasher.Hash(senha!);
                await _unitOfWork.Usuarios.InsertAsync(usuario);
                Log.Information(LogTemplates.UsuarioCriado, usuario.Login, usuario.Perfil.ToString());
            }
            else
            {
                var atual = await _unitOfWork.Usuarios.Query().FirstOrDefaultAsync(u => u.Id == usuario.Id);
                if (atual == null)
                {
                    Log.Warning("Tentativa de atualizar usuário inexistente {UsuarioId}", usuario.Id);
                    return Result.Falha<Usuario>("Usuario nao encontrado.");
                }

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

                await _unitOfWork.Usuarios.UpdateAsync(atual);
                usuario = atual;
                Log.Information("Usuário {Usuario} atualizado", usuario.Login);
            }

            await _unitOfWork.SaveChangesAsync();
            return Result.Ok(usuario);
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "UsuarioService.SalvarAsync", ex.Message);
            return Result.Falha<Usuario>($"Erro ao salvar usuário: {ex.Message}");
        }
    }

    public async Task<Result> RedefinirSenhaAsync(int usuarioId, string novaSenha)
    {
        try
        {
            var validacaoSenha = ValidarSenha(novaSenha, obrigatoria: true);
            if (!validacaoSenha.Sucesso)
            {
                Log.Warning("Falha na validação de senha para usuário {UsuarioId}: {Erro}", usuarioId, validacaoSenha.Erro);
                return validacaoSenha;
            }

            var usuario = await _unitOfWork.Usuarios.Query().FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null)
            {
                Log.Warning("Tentativa de redefinir senha de usuário inexistente {UsuarioId}", usuarioId);
                return Result.Falha("Usuario nao encontrado.");
            }

            usuario.SenhaHash = SenhaHasher.Hash(novaSenha);
            usuario.AtualizadoEm = DateTime.Now;
            await _unitOfWork.Usuarios.UpdateAsync(usuario);
            await _unitOfWork.SaveChangesAsync();

            Log.Information("Senha redefinida para usuário {Usuario}", usuario.Login);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "UsuarioService.RedefinirSenhaAsync", ex.Message);
            return Result.Falha($"Erro ao redefinir senha: {ex.Message}");
        }
    }

    public async Task<Result> AlternarAtivoAsync(int usuarioId)
    {
        try
        {
            var usuario = await _unitOfWork.Usuarios.Query().FirstOrDefaultAsync(u => u.Id == usuarioId);
            if (usuario == null)
            {
                Log.Warning("Tentativa de alternar status de usuário inexistente {UsuarioId}", usuarioId);
                return Result.Falha("Usuario nao encontrado.");
            }

            var copia = new Usuario
            {
                Id = usuario.Id,
                Login = usuario.Login,
                Nome = usuario.Nome,
                Perfil = usuario.Perfil,
                Ativo = !usuario.Ativo
            };

            if (!await MantemAdministradorAtivoAsync(copia))
            {
                Log.Warning("Tentativa de desativar único administrador {Usuario}", usuario.Login);
                return Result.Falha("E necessario manter ao menos um administrador ativo.");
            }

            usuario.Ativo = !usuario.Ativo;
            usuario.AtualizadoEm = DateTime.Now;
            await _unitOfWork.Usuarios.UpdateAsync(usuario);
            await _unitOfWork.SaveChangesAsync();

            var statusAtual = usuario.Ativo ? "ativado" : "inativado";
            Log.Information("Usuário {Usuario} {Status}", usuario.Login, statusAtual);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "UsuarioService.AlternarAtivoAsync", ex.Message);
            return Result.Falha($"Erro ao alternar status: {ex.Message}");
        }
    }

    private async Task<bool> MantemAdministradorAtivoAsync(Usuario usuario)
    {
        if (usuario.Ativo && usuario.Perfil == PerfilUsuario.Administrador)
            return true;

        return await _unitOfWork.Usuarios.Query().AnyAsync(u =>
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
