namespace ProjetoVarejo.Domain.Entities;

public class ItemPedidoCompra : EntidadeBase
{
    public int PedidoCompraId { get; set; }
    public int? ProdutoId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public decimal Quantidade { get; set; }
    public decimal QuantidadeRecebida { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal? ValorUnitarioRecebido { get; set; }
    public decimal Subtotal { get; set; }

    public PedidoCompra? PedidoCompra { get; set; }
    public Produto? Produto { get; set; }
}
