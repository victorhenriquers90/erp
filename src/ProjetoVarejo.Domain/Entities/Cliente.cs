namespace ProjetoVarejo.Domain.Entities;

public class Cliente : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? CpfCnpj { get; set; }
    public bool PessoaJuridica { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }

    public string? Cep { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }

    public decimal LimiteCredito { get; set; }
    public string? Observacao { get; set; }

    public ICollection<Venda> Vendas { get; set; } = new List<Venda>();
}
