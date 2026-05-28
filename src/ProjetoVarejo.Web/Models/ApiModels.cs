namespace ProjetoVarejo.Web.Models;

/// <summary>Generic wrapper matching the API's ApiResponse&lt;T&gt; response format.</summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
}

public sealed class ProdutoResumo
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoBarras { get; set; }
    public string? Descricao { get; set; }
    public string? Categoria { get; set; }
    public string? Unidade { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal Estoque { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public bool Ativo { get; set; }
}

public sealed class ClienteResumo
{
    public int Id { get; set; }
    public string? Nome { get; set; }
    public string? CpfCnpj { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
}

public sealed class EstoqueAlerta
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string? Descricao { get; set; }
    public decimal Estoque { get; set; }
    public decimal EstoqueMinimo { get; set; }
}

public sealed class MovimentoEstoque
{
    public int Id { get; set; }
    public DateTime CriadoEm { get; set; }
    public string? Produto { get; set; }
    public string? Tipo { get; set; }
    public decimal Quantidade { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoAtual { get; set; }
    public string? Documento { get; set; }
    public string? Observacao { get; set; }
    public string? Usuario { get; set; }
}

public sealed class VendaDiariaItem
{
    public DateTime Dia { get; set; }
    public int Quantidade { get; set; }
    public decimal Total { get; set; }
}

public sealed class ProdutoRankingItem
{
    public string? Codigo { get; set; }
    public string? Descricao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal Faturamento { get; set; }
    public string? Classe { get; set; }
}

public sealed class FornecedorResumo
{
    public int Id { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Cnpj { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Contato { get; set; }
    public bool Ativo { get; set; }
}

public sealed class ContaResumo
{
    public int Id { get; set; }
    public string? Tipo { get; set; }
    public string? Descricao { get; set; }
    public string? DocumentoNumero { get; set; }
    public DateTime DataVencimento { get; set; }
    public decimal Valor { get; set; }
    public string? Status { get; set; }
}

public sealed class ResumoFinanceiro
{
    public decimal TotalReceber { get; set; }
    public decimal TotalPagar { get; set; }
    public decimal SaldoPrevisto { get; set; }
    public decimal VendasHoje { get; set; }
}
