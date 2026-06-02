using FluentAssertions;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Xunit;

namespace ProjetoVarejo.Tests;

public class VendaServiceTests
{
    private static VendaService NewVendaService(TestDbFactory f) =>
        new(f.Db, f.Sessao, new EstoqueService(f.Db, f.Sessao));

    [Fact]
    public async Task FinalizarVenda_BaixaEstoque()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", estoque: 10, preco: 5);
        var svc = NewVendaService(f);

        var vRes = await svc.NovaVendaAsync();
        await svc.AdicionarItemAsync(vRes.Valor!.Id, produto.Id, 3);

        var fin = await svc.FinalizarAsync(vRes.Valor.Id, new List<PagamentoVenda>
        {
            new() { FormaPagamento = FormaPagamentoTipo.Dinheiro, Valor = 15 }
        });

        fin.Sucesso.Should().BeTrue();
        fin.Valor!.Status.Should().Be(StatusVenda.Finalizada);
        fin.Valor.Troco.Should().Be(0);

        f.Db.Entry(produto).Reload();
        produto.Estoque.Should().Be(7);
    }

    [Fact]
    public async Task FinalizarVenda_ValorPagoMenor_Falha()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", estoque: 10, preco: 10);
        var svc = NewVendaService(f);
        var v = (await svc.NovaVendaAsync()).Valor!;
        await svc.AdicionarItemAsync(v.Id, produto.Id, 2);

        var fin = await svc.FinalizarAsync(v.Id, new List<PagamentoVenda>
        {
            new() { FormaPagamento = FormaPagamentoTipo.Dinheiro, Valor = 5 }
        });

        fin.Sucesso.Should().BeFalse();
        fin.Erro.Should().Contain("menor");
    }

    [Fact]
    public async Task FinalizarVenda_CalculaTrocoCorretamente()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", estoque: 10, preco: 10);
        var svc = NewVendaService(f);
        var v = (await svc.NovaVendaAsync()).Valor!;
        await svc.AdicionarItemAsync(v.Id, produto.Id, 1);

        var fin = await svc.FinalizarAsync(v.Id, new List<PagamentoVenda>
        {
            new() { FormaPagamento = FormaPagamentoTipo.Dinheiro, Valor = 50 }
        });

        fin.Sucesso.Should().BeTrue();
        fin.Valor!.Troco.Should().Be(40);
    }

    [Fact]
    public async Task CancelarVenda_Finalizada_DevolveEstoque()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", estoque: 10, preco: 5);
        var svc = NewVendaService(f);
        var v = (await svc.NovaVendaAsync()).Valor!;
        await svc.AdicionarItemAsync(v.Id, produto.Id, 4);
        await svc.FinalizarAsync(v.Id, new List<PagamentoVenda>
        {
            new() { FormaPagamento = FormaPagamentoTipo.Dinheiro, Valor = 20 }
        });

        f.Db.Entry(produto).Reload();
        produto.Estoque.Should().Be(6);

        var canc = await svc.CancelarAsync(v.Id, "teste cancel");

        canc.Sucesso.Should().BeTrue();
        f.Db.Entry(produto).Reload();
        produto.Estoque.Should().Be(10);
    }

    [Fact]
    public async Task AdicionarItem_EstoqueInsuficiente_Falha()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", estoque: 2, preco: 5);
        var svc = NewVendaService(f);
        var v = (await svc.NovaVendaAsync()).Valor!;

        var res = await svc.AdicionarItemAsync(v.Id, produto.Id, 5);

        res.Sucesso.Should().BeFalse();
        res.Erro.Should().Contain("insuficiente");
    }

    [Fact]
    public async Task AdicionarItem_RecalculaTotal()
    {
        using var f = new TestDbFactory();
        var p1 = f.AdicionarProduto("P001", estoque: 10, preco: 5);
        var p2 = f.AdicionarProduto("P002", estoque: 10, preco: 3);
        var svc = NewVendaService(f);
        var v = (await svc.NovaVendaAsync()).Valor!;

        await svc.AdicionarItemAsync(v.Id, p1.Id, 2);
        await svc.AdicionarItemAsync(v.Id, p2.Id, 1);

        var venda = await svc.BuscarAsync(v.Id);
        venda!.SubTotal.Should().Be(13);
        venda.Total.Should().Be(13);
    }

    [Fact]
    public async Task AplicarDesconto_AjustaTotal()
    {
        using var f = new TestDbFactory();
        var produto = f.AdicionarProduto("P001", estoque: 10, preco: 10);
        var svc = NewVendaService(f);
        var v = (await svc.NovaVendaAsync()).Valor!;
        await svc.AdicionarItemAsync(v.Id, produto.Id, 5); // total 50

        var res = await svc.AplicarDescontoAsync(v.Id, 10);

        res.Sucesso.Should().BeTrue();
        var venda = await svc.BuscarAsync(v.Id);
        venda!.Total.Should().Be(40);
    }
}
