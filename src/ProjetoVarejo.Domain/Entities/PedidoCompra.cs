namespace ProjetoVarejo.Domain.Entities;

public class PedidoCompra : EntidadeBase
{
    public string Numero { get; set; } = string.Empty;
    public int FornecedorId { get; set; }
    public DateTime DataEmissao { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Rascunho";
    public decimal Total { get; set; }
    public string? Observacao { get; set; }

    public Fornecedor? Fornecedor { get; set; }
    public ICollection<ItemPedidoCompra> Itens { get; set; } = new List<ItemPedidoCompra>();
}
