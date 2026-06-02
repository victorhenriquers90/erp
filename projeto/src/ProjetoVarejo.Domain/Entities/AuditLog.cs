namespace ProjetoVarejo.Domain.Entities;

public enum TipoAuditoria
{
    Insert = 1,
    Update = 2,
    Delete = 3
}

public class AuditLog : EntidadeBase
{
    public DateTime Data { get; set; } = DateTime.Now;
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string Entidade { get; set; } = string.Empty;
    public string? RegistroId { get; set; }
    public TipoAuditoria Tipo { get; set; }
    public string? ValoresAntes { get; set; }
    public string? ValoresDepois { get; set; }
}
