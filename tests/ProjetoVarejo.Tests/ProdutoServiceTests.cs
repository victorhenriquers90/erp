using ProjetoVarejo.Application.Services;

namespace ProjetoVarejo.Tests;

public class ProdutoServiceTests
{
    [Fact]
    public async Task ListarParaVendaAsync_PesquisaPorDescricaoECodigo()
    {
        using var f = new TestDbFactory();
        f.AdicionarProduto("ARZ001", preco: 10).Descricao = "Arroz branco 5kg";
        f.AdicionarProduto("FEJ001", preco: 8).Descricao = "Feijao carioca 1kg";
        await f.Db.SaveChangesAsync();
        var svc = new ProdutoService(f.Db);

        var porNome = await svc.ListarParaVendaAsync("Arroz");
        var porCodigo = await svc.ListarParaVendaAsync("FEJ");

        Assert.Contains(porNome, p => p.Codigo == "ARZ001");
        Assert.Contains(porCodigo, p => p.Codigo == "FEJ001");
    }

    [Fact]
    public async Task BuscarPorCodigoAsync_IgnoraProdutoInativo()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", preco: 10);
        produto.CodigoBarras = "7890000000001";
        produto.Ativo = false;
        await f.Db.SaveChangesAsync();
        var svc = new ProdutoService(f.Db);

        var res = await svc.BuscarPorCodigoAsync("7890000000001");

        Assert.Null(res);
    }
}
