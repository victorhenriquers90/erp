using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Domain.Entities;

public class PagamentoVenda : EntidadeBase
{
    public int VendaId { get; set; }
    public Venda Venda { get; set; } = null!;
    public FormaPagamentoTipo FormaPagamento { get; set; }
    public decimal Valor { get; set; }
    public int Parcelas { get; set; } = 1;
    public string? Autorizacao { get; set; }
}
