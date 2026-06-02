using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Domain.Entities;

public class Usuario : EntidadeBase
{
    public string Login { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; }
    public DateTime? UltimoAcesso { get; set; }
}
