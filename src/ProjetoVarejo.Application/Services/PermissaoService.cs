using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class PermissaoService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SessaoApp _sessao;

    public PermissaoService(IUnitOfWork unitOfWork, SessaoApp sessao)
    {
        _unitOfWork = unitOfWork; _sessao = sessao;
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
        var customizada = await _unitOfWork.UsuarioPermissoes.Query()
            .AnyAsync(p => p.UsuarioId == usuarioId && p.Permissao == permissao);
        if (customizada) return true;
        return PermissoesPadrao.TryGetValue(perfil, out var lista) && lista.Contains(permissao);
    }

    public async Task<HashSet<Permissao>> ObterPermissoesAsync(Usuario usuario)
    {
        var padrao = PermissoesPadrao.TryGetValue(usuario.Perfil, out var p) ? p : new();
        var custom = await _unitOfWork.UsuarioPermissoes.Query()
            .Where(up => up.UsuarioId == usuario.Id)
            .Select(up => up.Permissao)
            .ToListAsync();
        return new HashSet<Permissao>(padrao.Concat(custom));
    }

    public async Task<Result> ConcederAsync(int usuarioId, Permissao permissao)
    {
        var existe = await _unitOfWork.UsuarioPermissoes.Query()
            .AnyAsync(p => p.UsuarioId == usuarioId && p.Permissao == permissao);
        if (existe) return Result.Ok();
        await _unitOfWork.UsuarioPermissoes.InsertAsync(new UsuarioPermissao
        {
            UsuarioId = usuarioId,
            Permissao = permissao
        });
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> RevogarAsync(int usuarioId, Permissao permissao)
    {
        var p = await _unitOfWork.UsuarioPermissoes.Query()
            .FirstOrDefaultAsync(x => x.UsuarioId == usuarioId && x.Permissao == permissao);
        if (p != null)
        {
            await _unitOfWork.UsuarioPermissoes.DeleteAsync(p);
            await _unitOfWork.SaveChangesAsync();
        }
        return Result.Ok();
    }
}
