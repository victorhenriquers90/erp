using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Domain.Entities;

public class Venda : EntidadeBase
{
    public string Numero { get; set; } = string.Empty;
    public DateTime DataVenda { get; set; } = DateTime.Now;
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;

    public decimal SubTotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Acrescimo { get; set; }
    public decimal Total { get; set; }
    public decimal ValorPago { get; set; }
    public decimal Troco { get; set; }

    public StatusVenda Status { get; set; } = StatusVenda.EmAberto;
    public string? Observacao { get; set; }
    public DateTime? FinalizadaEm { get; set; }
    public DateTime? CanceladaEm { get; set; }

    public int? NotaFiscalId { get; set; }
    public NotaFiscal? NotaFiscal { get; set; }

    public ICollection<ItemVenda> Itens { get; set; } = new List<ItemVenda>();
    public ICollection<PagamentoVenda> Pagamentos { get; set; } = new List<PagamentoVenda>();
}
