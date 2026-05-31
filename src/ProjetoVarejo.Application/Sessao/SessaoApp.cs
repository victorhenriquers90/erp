using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Sessao;

/// <summary>
/// Gerencia a sessão do usuário com suporte a timeout automático
/// </summary>
public class SessaoApp
{
    private DateTime? _ultimaAtividade;
    private readonly TimeSpan _tempoTimeout = TimeSpan.FromMinutes(30); // 30 minutos

    public Usuario? UsuarioLogado { get; private set; }
    public CaixaSessao? CaixaAtual { get; set; }
    public EmpresaConfig? EmpresaAtiva { get; set; }
    public Filial? FilialAtiva { get; set; }

    /// <summary>
    /// Hora da última atividade do usuário. Atualizado a cada ação
    /// </summary>
    public DateTime? UltimaAtividade
    {
        get => _ultimaAtividade;
        set => _ultimaAtividade = value;
    }

    public bool Autenticado => UsuarioLogado != null && !Expirou();

    /// <summary>
    /// Verifica se a sessão expirou por inatividade
    /// </summary>
    public bool Expirou()
    {
        if (UsuarioLogado == null || _ultimaAtividade == null)
            return false;

        var agora = DateTime.Now;
        var tempoDecorrido = agora - _ultimaAtividade.Value;
        return tempoDecorrido > _tempoTimeout;
    }

    public void DefinirUsuario(Usuario usuario)
    {
        UsuarioLogado = usuario;
        _ultimaAtividade = DateTime.Now; // Marca hora do login
    }

    /// <summary>
    /// Atualiza a hora da última atividade para evitar timeout
    /// Deve ser chamada a cada ação do usuário
    /// </summary>
    public void AtualizarAtividade()
    {
        if (UsuarioLogado != null)
            _ultimaAtividade = DateTime.Now;
    }

    public void Logout()
    {
        UsuarioLogado = null;
        CaixaAtual = null;
        EmpresaAtiva = null;
        FilialAtiva = null;
        _ultimaAtividade = null;
    }
}
