namespace ProjetoVarejo.Domain.Entities;

public class CaixaSessao : EntidadeBase
{
    public int UsuarioAberturaId { get; set; }
    public Usuario UsuarioAbertura { get; set; } = null!;
    public DateTime AbertaEm { get; set; } = DateTime.Now;
    public DateTime? FechadaEm { get; set; }
    public int? UsuarioFechamentoId { get; set; }
    public Usuario? UsuarioFechamento { get; set; }
    public decimal ValorAbertura { get; set; }
    public decimal ValorFechamentoInformado { get; set; }
    public decimal ValorFechamentoCalculado { get; set; }
    public decimal Diferenca { get; set; }
    public string? Observacao { get; set; }
    public bool Aberta => FechadaEm == null;
}
