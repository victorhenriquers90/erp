using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Domain.Entities;

public class ContaFinanceira : EntidadeBase
{
    public TipoConta Tipo { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? DocumentoNumero { get; set; }
    public DateTime DataEmissao { get; set; } = DateTime.Now;
    public DateTime DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public decimal Valor { get; set; }
    public decimal ValorPago { get; set; }
    public decimal Juros { get; set; }
    public decimal Multa { get; set; }
    public decimal Desconto { get; set; }
    public StatusConta Status { get; set; } = StatusConta.EmAberto;
    public FormaPagamentoTipo? FormaPagamento { get; set; }
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }
    public int? FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }
    public int? VendaId { get; set; }
    public Venda? Venda { get; set; }
    public string? Observacao { get; set; }
}
