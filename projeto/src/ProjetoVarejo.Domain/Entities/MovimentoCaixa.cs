using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Domain.Entities;

public enum TipoMovimentoCaixa
{
    Abertura = 1,
    Sangria = 2,
    Suprimento = 3,
    Venda = 4,
    Fechamento = 5
}

public class MovimentoCaixa : EntidadeBase
{
    public int CaixaSessaoId { get; set; }
    public CaixaSessao CaixaSessao { get; set; } = null!;
    public TipoMovimentoCaixa Tipo { get; set; }
    public DateTime Data { get; set; } = DateTime.Now;
    public decimal Valor { get; set; }
    public FormaPagamentoTipo? FormaPagamento { get; set; }
    public int? VendaId { get; set; }
    public Venda? Venda { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string? Observacao { get; set; }
}
