using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Sessao;

public class SessaoApp
{
    public Usuario? UsuarioLogado { get; private set; }
    public CaixaSessao? CaixaAtual { get; set; }
    public EmpresaConfig? EmpresaAtiva { get; set; }

    public bool Autenticado => UsuarioLogado != null;

    public void DefinirUsuario(Usuario usuario) => UsuarioLogado = usuario;
    public void Logout()
    {
        UsuarioLogado = null;
        CaixaAtual = null;
        EmpresaAtiva = null;
    }
}
