namespace ProjetoVarejo.Domain.Entities;

public abstract class EntidadeBase
{
    public int Id { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
    public DateTime? AtualizadoEm { get; set; }
    public bool Ativo { get; set; } = true;
}
