namespace ProjetoVarejo.Domain.Entities;

public class Categoria : EntidadeBase
{
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }

    public ICollection<Produto> Produtos { get; set; } = new List<Produto>();

    public override string ToString() => Nome;
}
