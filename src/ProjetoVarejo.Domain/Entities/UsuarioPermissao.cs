using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Domain.Entities;

public class UsuarioPermissao : EntidadeBase
{
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public Permissao Permissao { get; set; }
}
