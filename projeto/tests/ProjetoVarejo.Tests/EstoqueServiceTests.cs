using FluentAssertions;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Shared;
using Xunit;

namespace ProjetoVarejo.Tests;

public class EstoqueServiceTests
{
    [Fact]
    public async Task RegistrarMovimento_Entrada_AumentaEstoqueEAtualizaCusto()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", estoque: 5);
        var svc = new EstoqueService(f.Db, f.Sessao);

        var res = await svc.RegistrarMovimentoAsync(produto.Id, TipoMovimentoEstoque.Entrada, 10, custoUnitario: 25);

        res.Sucesso.Should().BeTrue();
        f.Db.Entry(produto).Reload();
        produto.Estoque.Should().Be(15);
        produto.PrecoCusto.Should().Be(25);
    }

    [Fact]
    public async Task RegistrarMovimento_Saida_DiminuiEstoque()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", estoque: 10);
        var svc = new EstoqueService(f.Db, f.Sessao);

        var res = await svc.RegistrarMovimentoAsync(produto.Id, TipoMovimentoEstoque.Saida, 3);

        res.Sucesso.Should().BeTrue();
        f.Db.Entry(produto).Reload();
        produto.Estoque.Should().Be(7);
    }

    [Fact]
    public async Task RegistrarMovimento_SaidaSemEstoque_Falha()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", estoque: 2);
        var svc = new EstoqueService(f.Db, f.Sessao);

        var res = await svc.RegistrarMovimentoAsync(produto.Id, TipoMovimentoEstoque.Saida, 5);

        res.Sucesso.Should().BeFalse();
        res.Erro.Should().Contain("insuficiente");
        f.Db.Entry(produto).Reload();
        produto.Estoque.Should().Be(2); // não mudou
    }

    [Fact]
    public async Task RegistrarMovimento_SemControleEstoque_PermiteSaidaNegativa()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", estoque: 0, controla: false);
        var svc = new EstoqueService(f.Db, f.Sessao);

        var res = await svc.RegistrarMovimentoAsync(produto.Id, TipoMovimentoEstoque.Saida, 5);

        res.Sucesso.Should().BeTrue();
    }

    [Fact]
    public async Task RegistrarMovimento_QuantidadeZero_Falha()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001");
        var svc = new EstoqueService(f.Db, f.Sessao);

        var res = await svc.RegistrarMovimentoAsync(produto.Id, TipoMovimentoEstoque.Entrada, 0);

        res.Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task ProdutosAbaixoMinimo_RetornaApenasOsAbaixo()
    {
        using var f = new TestDbFactory();
        var p1 = f.AdicionarProduto("P001", estoque: 5);
        p1.EstoqueMinimo = 10;
        var p2 = f.AdicionarProduto("P002", estoque: 20);
        p2.EstoqueMinimo = 5;
        f.Db.SaveChanges();

        var svc = new EstoqueService(f.Db, f.Sessao);
        var lista = await svc.ProdutosAbaixoMinimoAsync();

        lista.Should().HaveCount(1);
        lista[0].Codigo.Should().Be("P001");
    }
}
