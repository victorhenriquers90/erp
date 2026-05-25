namespace ProjetoVarejo.Infrastructure.Marketplace;

public class ProdutoMarketplace
{
    public string IdLocal { get; set; } = "";
    public string IdMarketplace { get; set; } = "";
    public string Titulo { get; set; } = "";
    public decimal Preco { get; set; }
    public int Estoque { get; set; }
    public string? Categoria { get; set; }
}

public class PedidoMarketplace
{
    public string IdMarketplace { get; set; } = "";
    public DateTime Data { get; set; }
    public string CompradorNome { get; set; } = "";
    public decimal Total { get; set; }
    public string Status { get; set; } = "";
    public List<ProdutoMarketplace> Itens { get; set; } = new();
}

public interface IMarketplaceConnector
{
    string Nome { get; }
    string ObterUrlAutorizacao();
    Task<bool> TrocarCodeAsync(string code);
    Task<List<PedidoMarketplace>> ListarPedidosRecentesAsync(int dias = 7);
    Task<bool> AtualizarEstoqueAsync(string idMarketplace, int novoEstoque);
}
