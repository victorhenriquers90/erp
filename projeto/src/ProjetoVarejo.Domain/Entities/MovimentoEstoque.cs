using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Domain.Entities;

public class MovimentoEstoque : EntidadeBase
{
    public int ProdutoId { get; set; }
    public Produto Produto { get; set; } = null!;
    public TipoMovimentoEstoque Tipo { get; set; }
    public decimal Quantidade { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoAtual { get; set; }
    public decimal? CustoUnitario { get; set; }
    public string? Documento { get; set; }
    public int? VendaId { get; set; }
    public Venda? Venda { get; set; }
    public int? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string? Observacao { get; set; }
}
