using FluentAssertions;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Xunit;

namespace ProjetoVarejo.Tests;

public class CaixaServiceTests
{
    [Fact]
    public async Task Abrir_RegistraMovimentoAbertura()
    {
        using var f = new TestDbFactory();
        var svc = new CaixaService(f.Db, f.Sessao);

        var res = await svc.AbrirAsync(100);

        res.Sucesso.Should().BeTrue();
        var movs = f.Db.MovimentosCaixa.ToList();
        movs.Should().HaveCount(1);
        movs[0].Tipo.Should().Be(TipoMovimentoCaixa.Abertura);
        movs[0].Valor.Should().Be(100);
    }

    [Fact]
    public async Task Abrir_DuasVezes_Falha()
    {
        using var f = new TestDbFactory();
        var svc = new CaixaService(f.Db, f.Sessao);
        await svc.AbrirAsync(100);

        var res = await svc.AbrirAsync(200);

        res.Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Sangria_DiminuiSaldoEsperado()
    {
        using var f = new TestDbFactory();
        var svc = new CaixaService(f.Db, f.Sessao);
        var caixa = (await svc.AbrirAsync(100)).Valor!;

        await svc.SangriaAsync(30, "retirada");

        var resumo = await svc.ResumoAsync(caixa.Id);
        resumo.TotalSangrias.Should().Be(30);
        resumo.SaldoDinheiroEsperado.Should().Be(70); // 100 - 30
    }

    [Fact]
    public async Task Suprimento_AumentaSaldoEsperado()
    {
        using var f = new TestDbFactory();
        var svc = new CaixaService(f.Db, f.Sessao);
        var caixa = (await svc.AbrirAsync(100)).Valor!;

        await svc.SuprimentoAsync(50, "troco");

        var resumo = await svc.ResumoAsync(caixa.Id);
        resumo.TotalSuprimentos.Should().Be(50);
        resumo.SaldoDinheiroEsperado.Should().Be(150);
    }

    private static int CriarVendaFake(TestDbFactory f)
    {
        var v = new Venda { Numero = "T" + Guid.NewGuid().ToString("N")[..8], UsuarioId = f.Sessao.UsuarioLogado!.Id, Status = StatusVenda.Finalizada };
        f.Db.Vendas.Add(v);
        f.Db.SaveChanges();
        return v.Id;
    }

    [Fact]
    public async Task RegistrarVenda_AgrupaPorFormaPagamento()
    {
        using var f = new TestDbFactory();
        var svc = new CaixaService(f.Db, f.Sessao);
        var caixa = (await svc.AbrirAsync(0)).Valor!;
        var v1 = CriarVendaFake(f);
        var v2 = CriarVendaFake(f);

        await svc.RegistrarVendaAsync(caixa.Id, v1, new[]
        {
            new PagamentoVenda { FormaPagamento = FormaPagamentoTipo.Dinheiro, Valor = 50 },
            new PagamentoVenda { FormaPagamento = FormaPagamentoTipo.Credito, Valor = 100 }
        });
        await svc.RegistrarVendaAsync(caixa.Id, v2, new[]
        {
            new PagamentoVenda { FormaPagamento = FormaPagamentoTipo.Dinheiro, Valor = 30 }
        });

        var r = await svc.ResumoAsync(caixa.Id);
        r.VendasPorForma[FormaPagamentoTipo.Dinheiro].Should().Be(80);
        r.VendasPorForma[FormaPagamentoTipo.Credito].Should().Be(100);
        r.TotalVendas.Should().Be(180);
        r.SaldoDinheiroEsperado.Should().Be(80); // só dinheiro
    }

    [Fact]
    public async Task Fechar_CalculaDiferenca()
    {
        using var f = new TestDbFactory();
        var svc = new CaixaService(f.Db, f.Sessao);
        var caixa = (await svc.AbrirAsync(100)).Valor!;
        var v = CriarVendaFake(f);
        await svc.RegistrarVendaAsync(caixa.Id, v, new[]
        {
            new PagamentoVenda { FormaPagamento = FormaPagamentoTipo.Dinheiro, Valor = 50 }
        });
        // esperado = 100 + 50 = 150. Operador conta 145 → falta 5.

        var res = await svc.FecharAsync(caixa.Id, 145);

        res.Sucesso.Should().BeTrue();
        res.Valor!.Diferenca.Should().Be(-5);
        res.Valor.ValorFechamentoCalculado.Should().Be(150);
    }
}
