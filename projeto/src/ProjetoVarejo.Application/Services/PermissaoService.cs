using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class PermissaoService
{
    private readonly AppDbContext _db;
    private readonly SessaoApp _sessao;

    public PermissaoService(AppDbContext db, SessaoApp sessao)
    {
        _db = db; _sessao = sessao;
    }

    public static readonly Dictionary<PerfilUsuario, HashSet<Permissao>> PermissoesPadrao = new()
    {
        [PerfilUsuario.Administrador] = new HashSet<Permissao>((Permissao[])Enum.GetValues(typeof(Permissao))),
        [PerfilUsuario.Gerente] = new HashSet<Permissao>
        {
            Permissao.AbrirPdv, Permissao.AplicarDesconto, Permissao.CancelarVenda,
            Permissao.EmitirNfce, Permissao.CancelarNfce, Permissao.InutilizarNfce,
            Permissao.AbrirCaixa, Permissao.FecharCaixa, Permissao.Sangria, Permissao.Suprimento,
            Permissao.GerenciarProdutos, Permissao.GerenciarClientes, Permissao.GerenciarFornecedores,
            Permissao.LancarEntradaEstoque, Permissao.LancarSaidaEstoque, Permissao.ImportarXmlNfe,
            Permissao.GerenciarContas, Permissao.QuitarContas,
            Permissao.AcessarRelatorios, Permissao.ExecutarBackup
        },
        [PerfilUsuario.Caixa] = new HashSet<Permissao>
        {
            Permissao.AbrirPdv, Permissao.EmitirNfce,
            Permissao.AbrirCaixa, Permissao.FecharCaixa, Permissao.Sangria, Permissao.Suprimento
        },
        [PerfilUsuario.Estoquista] = new HashSet<Permissao>
        {
            Permissao.GerenciarProdutos,
            Permissao.LancarEntradaEstoque, Permissao.LancarSaidaEstoque, Permissao.ImportarXmlNfe
        }
    };

    public async Task<bool> TemPermissaoAsync(Permissao permissao)
    {
        if (_sessao.UsuarioLogado == null) return false;
        return await TemPermissaoAsync(_sessao.UsuarioLogado.Id, _sessao.UsuarioLogado.Perfil, permissao);
    }

    public async Task<bool> TemPermissaoAsync(int usuarioId, PerfilUsuario perfil, Permissao permissao)
    {
        var customizada = await _db.UsuarioPermissoes
            .AnyAsync(p => p.UsuarioId == usuarioId && p.Permissao == permissao);
        if (customizada) return true;
        return PermissoesPadrao.TryGetValue(perfil, out var lista) && lista.Contains(permissao);
    }

    public async Task<HashSet<Permissao>> ObterPermissoesAsync(Usuario usuario)
    {
        var padrao = PermissoesPadrao.TryGetValue(usuario.Perfil, out var p) ? p : new();
        var custom = await _db.UsuarioPermissoes
            .Where(up => up.UsuarioId == usuario.Id)
            .Select(up => up.Permissao)
            .ToListAsync();
        return new HashSet<Permissao>(padrao.Concat(custom));
    }

    public async Task<Result> ConcederAsync(int usuarioId, Permissao permissao)
    {
        var existe = await _db.UsuarioPermissoes
            .AnyAsync(p => p.UsuarioId == usuarioId && p.Permissao == permissao);
        if (existe) return Result.Ok();
        _db.UsuarioPermissoes.Add(new UsuarioPermissao
        {
            UsuarioId = usuarioId,
            Permissao = permissao
        });
        await _db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> RevogarAsync(int usuarioId, Permissao permissao)
    {
        var p = await _db.UsuarioPermissoes
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.Permissao == permissao);
        if (p != null)
        {
            _db.UsuarioPermissoes.Remove(p);
            await _db.SaveChangesAsync();
        }
        return Result.Ok();
    }

    public async Task<Result<Usuario>> AutorizarSupervisorAsync(string login, string senha, Permissao permissao)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
            return Result.Falha<Usuario>("Informe login e senha do supervisor.");

        var usuario = await _db.Usuarios.FirstOrDefaultAsync(u => u.Login == login && u.Ativo);
        if (usuario == null)
            return Result.Falha<Usuario>("Supervisor não encontrado.");

        if (!SenhaHasher.Verifica(senha, usuario.SenhaHash))
            return Result.Falha<Usuario>("Senha do supervisor inválida.");

        if (usuario.Perfil is not (PerfilUsuario.Administrador or PerfilUsuario.Gerente))
            return Result.Falha<Usuario>("Usuário informado não possui perfil de supervisor.");

        var autorizado = await TemPermissaoAsync(usuario.Id, usuario.Perfil, permissao);
        if (!autorizado)
            return Result.Falha<Usuario>("Supervisor sem permissão para autorizar esta operação.");

        return Result.Ok(usuario);
    }
}
