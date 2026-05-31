using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Domain.Entities;

public class Usuario : EntidadeBase
{
    public string Login { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    public DateTime? UltimoAcesso { get; set; }

    /// <summary>
    /// Filial à qual este usuário pertence.
    /// null = acesso irrestrito (Administrador / multi-filial).
    /// </summary>
    public int? FilialId { get; set; }
    public Filial? Filial { get; set; }
}
