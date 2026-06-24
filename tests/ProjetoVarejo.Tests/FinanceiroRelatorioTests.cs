using FluentAssertions;
using Moq;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using ProjetoVarejo.Tests.Helpers;
using Xunit;

namespace ProjetoVarejo.Tests;

/// <summary>
/// Testes unitários para FinanceiroService cobrindo cenários de relatório/resumo,
/// validação de salvamento e quitação de contas financeiras.
/// </summary>
public class FinanceiroRelatorioTests
{
    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private static (FinanceiroService svc, Mock<IUnitOfWork> uow) CriarServico(
        List<ContaFinanceira>? contas = null,
        List<Venda>? vendas = null)
    {
        contas ??= new List<ContaFinanceira>();
        vendas ??= new List<Venda>();

        var uow = new Mock<IUnitOfWork>();

        var repoContas = new Mock<IRepository<ContaFinanceira>>();
        repoContas.Setup(r => r.Query()).Returns(contas.AsAsyncQueryable());

        var repoVendas = new Mock<IRepository<Venda>>();
        repoVendas.Setup(r => r.Query()).Returns(vendas.AsAsyncQueryable());

        uow.Setup(u => u.ContasFinanceiras).Returns(repoContas.Object);
        uow.Setup(u => u.Vendas).Returns(repoVendas.Object);
        uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var svc = new FinanceiroService(uow.Object);
        return (svc, uow);
    }

    private static ContaFinanceira NovaContaReceber(decimal valor, DateTime vencimento, StatusConta status = StatusConta.EmAberto) =>
        new()
        {
            Id = 0,
            Tipo = TipoConta.Receber,
            Descricao = "Fatura cliente",
            Valor = valor,
            DataVencimento = vencimento,
            Status = status
        };

    private static ContaFinanceira NovaContaPagar(decimal valor, DateTime vencimento, StatusConta status = StatusConta.EmAberto) =>
        new()
        {
            Id = 0,
            Tipo = TipoConta.Pagar,
            Descricao = "Fatura fornecedor",
            Valor = valor,
            DataVencimento = vencimento,
            Status = status
        };

    // ──────────────────────────────────────────────────────────────
    // ResumoAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResumoAsync_SemContas_RetornaZeros()
    {
        var (svc, _) = CriarServico();
        var de = new DateTime(2025, 1, 1);
        var ate = new DateTime(2025, 1, 31);

        var (receber, pagar, saldo) = await svc.ResumoAsync(de, ate);

        receber.Should().Be(0);
        pagar.Should().Be(0);
        saldo.Should().Be(0);
    }

    [Fact]
    public async Task ResumoAsync_ComContasReceber_SomaCorretamente()
    {
        var de = new DateTime(2025, 6, 1);
        var ate = new DateTime(2025, 6, 30);

        var contas = new List<ContaFinanceira>
        {
            NovaContaReceber(1000m, new DateTime(2025, 6, 10)),
            NovaContaReceber(500m,  new DateTime(2025, 6, 20)),
        };

        var (svc, _) = CriarServico(contas);

        var (receber, pagar, saldo) = await svc.ResumoAsync(de, ate);

        receber.Should().Be(1500m);
        pagar.Should().Be(0m);
        saldo.Should().Be(1500m);
    }

    [Fact]
    public async Task ResumoAsync_ComContasPagar_SomaCorretamente()
    {
        var de = new DateTime(2025, 6, 1);
        var ate = new DateTime(2025, 6, 30);

        var contas = new List<ContaFinanceira>
        {
            NovaContaPagar(800m, new DateTime(2025, 6, 5)),
            NovaContaPagar(200m, new DateTime(2025, 6, 25)),
        };

        var (svc, _) = CriarServico(contas);

        var (receber, pagar, saldo) = await svc.ResumoAsync(de, ate);

        pagar.Should().Be(1000m);
        receber.Should().Be(0m);
        saldo.Should().Be(-1000m);
    }

    [Fact]
    public async Task ResumoAsync_ComRecebereEPagar_CalculaSaldoPrevisto()
    {
        var de = new DateTime(2025, 6, 1);
        var ate = new DateTime(2025, 6, 30);

        var contas = new List<ContaFinanceira>
        {
            NovaContaReceber(3000m, new DateTime(2025, 6, 15)),
            NovaContaPagar(1200m,  new DateTime(2025, 6, 10)),
        };

        var (svc, _) = CriarServico(contas);

        var (receber, pagar, saldo) = await svc.ResumoAsync(de, ate);

        receber.Should().Be(3000m);
        pagar.Should().Be(1200m);
        saldo.Should().Be(1800m);
    }

    [Fact]
    public async Task ResumoAsync_IgnoraContasPagas()
    {
        var de = new DateTime(2025, 6, 1);
        var ate = new DateTime(2025, 6, 30);

        var contas = new List<ContaFinanceira>
        {
            NovaContaReceber(1000m, new DateTime(2025, 6, 10), StatusConta.Paga),
            NovaContaReceber(500m,  new DateTime(2025, 6, 20), StatusConta.EmAberto),
        };

        var (svc, _) = CriarServico(contas);

        var (receber, pagar, saldo) = await svc.ResumoAsync(de, ate);

        receber.Should().Be(500m, "apenas contas em aberto ou atrasadas devem entrar no resumo");
    }

    [Fact]
    public async Task ResumoAsync_IncluiContasAtrasadas()
    {
        var de = new DateTime(2025, 5, 1);
        var ate = new DateTime(2025, 5, 31);

        var contas = new List<ContaFinanceira>
        {
            NovaContaPagar(700m, new DateTime(2025, 5, 5), StatusConta.Atrasada),
        };

        var (svc, _) = CriarServico(contas);

        var (_, pagar, _) = await svc.ResumoAsync(de, ate);

        pagar.Should().Be(700m, "contas atrasadas devem ser incluídas no resumo");
    }

    [Fact]
    public async Task ResumoAsync_IgnoraContasForaDoIntervalo()
    {
        var de = new DateTime(2025, 6, 1);
        var ate = new DateTime(2025, 6, 30);

        var contas = new List<ContaFinanceira>
        {
            NovaContaReceber(999m, new DateTime(2025, 5, 31)), // antes do intervalo
            NovaContaReceber(999m, new DateTime(2025, 7, 1)),  // depois do intervalo
            NovaContaReceber(500m, new DateTime(2025, 6, 15)), // dentro do intervalo
        };

        var (svc, _) = CriarServico(contas);

        var (receber, _, _) = await svc.ResumoAsync(de, ate);

        receber.Should().Be(500m);
    }

    // ──────────────────────────────────────────────────────────────
    // TotalVendasDoDiaAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task TotalVendasDoDiaAsync_SemVendas_RetornaZero()
    {
        var (svc, _) = CriarServico();

        var total = await svc.TotalVendasDoDiaAsync(DateTime.Today);

        total.Should().Be(0m);
    }

    [Fact]
    public async Task TotalVendasDoDiaAsync_SomaApenasVendasFinalizadasNoDia()
    {
        var dia = new DateTime(2025, 6, 15);

        var vendas = new List<Venda>
        {
            new() { Total = 200m, Status = StatusVenda.Finalizada, FinalizadaEm = dia.AddHours(9) },
            new() { Total = 350m, Status = StatusVenda.Finalizada, FinalizadaEm = dia.AddHours(14) },
            new() { Total = 100m, Status = StatusVenda.Cancelada,  FinalizadaEm = dia.AddHours(10) }, // cancelada – não conta
            new() { Total = 500m, Status = StatusVenda.Finalizada, FinalizadaEm = dia.AddDays(-1) },  // dia anterior – não conta
        };

        var (svc, _) = CriarServico(vendas: vendas);

        var total = await svc.TotalVendasDoDiaAsync(dia);

        total.Should().Be(550m);
    }

    [Fact]
    public async Task TotalVendasDoDiaAsync_NaoIncluiVendaDoProximoDia()
    {
        var dia = new DateTime(2025, 6, 15);

        var vendas = new List<Venda>
        {
            new() { Total = 100m, Status = StatusVenda.Finalizada, FinalizadaEm = dia.Date.AddDays(1) }, // início do próximo dia
        };

        var (svc, _) = CriarServico(vendas: vendas);

        var total = await svc.TotalVendasDoDiaAsync(dia);

        total.Should().Be(0m, "o limite superior do dia é exclusivo (< dia+1)");
    }

    // ──────────────────────────────────────────────────────────────
    // SalvarAsync – validações
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SalvarAsync_SemDescricao_RetornaFalha()
    {
        var (svc, _) = CriarServico();
        var conta = new ContaFinanceira { Descricao = "", Valor = 100m, DataVencimento = DateTime.Today.AddDays(10) };

        var result = await svc.SalvarAsync(conta);

        result.Sucesso.Should().BeFalse();
        result.Erro.Should().Contain("Descrição");
    }

    [Fact]
    public async Task SalvarAsync_ValorZero_RetornaFalha()
    {
        var (svc, _) = CriarServico();
        var conta = new ContaFinanceira { Descricao = "Aluguel", Valor = 0m, DataVencimento = DateTime.Today.AddDays(5) };

        var result = await svc.SalvarAsync(conta);

        result.Sucesso.Should().BeFalse();
        result.Erro.Should().Contain("Valor");
    }

    [Fact]
    public async Task SalvarAsync_ValorNegativo_RetornaFalha()
    {
        var (svc, _) = CriarServico();
        var conta = new ContaFinanceira { Descricao = "Aluguel", Valor = -50m, DataVencimento = DateTime.Today.AddDays(5) };

        var result = await svc.SalvarAsync(conta);

        result.Sucesso.Should().BeFalse();
        result.Erro.Should().Contain("Valor");
    }

    [Fact]
    public async Task SalvarAsync_SemDataVencimento_RetornaFalha()
    {
        var (svc, _) = CriarServico();
        var conta = new ContaFinanceira { Descricao = "Salário", Valor = 3000m, DataVencimento = default };

        var result = await svc.SalvarAsync(conta);

        result.Sucesso.Should().BeFalse();
        result.Erro.Should().Contain("vencimento");
    }

    [Fact]
    public async Task SalvarAsync_ContaValida_Nova_RetornaOk()
    {
        var uow = new Mock<IUnitOfWork>();
        var repoContas = new Mock<IRepository<ContaFinanceira>>();

        ContaFinanceira? contaInserida = null;
        repoContas
            .Setup(r => r.InsertAsync(It.IsAny<ContaFinanceira>()))
            .Callback<ContaFinanceira>(c => contaInserida = c)
            .ReturnsAsync((ContaFinanceira c) => c);

        uow.Setup(u => u.ContasFinanceiras).Returns(repoContas.Object);
        uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var svc = new FinanceiroService(uow.Object);
        var conta = new ContaFinanceira { Id = 0, Descricao = "Mensalidade", Valor = 250m, DataVencimento = DateTime.Today.AddDays(15) };

        var result = await svc.SalvarAsync(conta);

        result.Sucesso.Should().BeTrue();
        contaInserida.Should().NotBeNull();
        uow.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SalvarAsync_ContaValida_Existente_ChamaUpdate()
    {
        var uow = new Mock<IUnitOfWork>();
        var repoContas = new Mock<IRepository<ContaFinanceira>>();

        repoContas
            .Setup(r => r.UpdateAsync(It.IsAny<ContaFinanceira>()))
            .ReturnsAsync((ContaFinanceira c) => c);

        uow.Setup(u => u.ContasFinanceiras).Returns(repoContas.Object);
        uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var svc = new FinanceiroService(uow.Object);
        var conta = new ContaFinanceira { Id = 5, Descricao = "Mensalidade", Valor = 250m, DataVencimento = DateTime.Today.AddDays(15) };

        var result = await svc.SalvarAsync(conta);

        result.Sucesso.Should().BeTrue();
        repoContas.Verify(r => r.UpdateAsync(conta), Times.Once);
        repoContas.Verify(r => r.InsertAsync(It.IsAny<ContaFinanceira>()), Times.Never);
    }

    // ──────────────────────────────────────────────────────────────
    // QuitarAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task QuitarAsync_ContaNaoEncontrada_RetornaFalha()
    {
        var uow = new Mock<IUnitOfWork>();
        var repoContas = new Mock<IRepository<ContaFinanceira>>();
        repoContas.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ContaFinanceira?)null);
        uow.Setup(u => u.ContasFinanceiras).Returns(repoContas.Object);

        var svc = new FinanceiroService(uow.Object);

        var result = await svc.QuitarAsync(99, DateTime.Today, 100m, FormaPagamentoTipo.Dinheiro);

        result.Sucesso.Should().BeFalse();
        result.Erro.Should().Contain("99");
    }

    [Fact]
    public async Task QuitarAsync_ContaJaPaga_RetornaFalha()
    {
        var conta = new ContaFinanceira { Id = 1, Status = StatusConta.Paga, Descricao = "X", Valor = 100m };

        var uow = new Mock<IUnitOfWork>();
        var repoContas = new Mock<IRepository<ContaFinanceira>>();
        repoContas.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(conta);
        uow.Setup(u => u.ContasFinanceiras).Returns(repoContas.Object);

        var svc = new FinanceiroService(uow.Object);

        var result = await svc.QuitarAsync(1, DateTime.Today, 100m, FormaPagamentoTipo.Pix);

        result.Sucesso.Should().BeFalse();
        result.Erro.Should().Contain("quitada");
    }

    [Fact]
    public async Task QuitarAsync_ContaEmAberto_AtualizaStatusParaPaga()
    {
        var conta = new ContaFinanceira
        {
            Id = 2,
            Status = StatusConta.EmAberto,
            Descricao = "Energia",
            Valor = 300m,
            DataVencimento = DateTime.Today
        };

        var uow = new Mock<IUnitOfWork>();
        var repoContas = new Mock<IRepository<ContaFinanceira>>();
        repoContas.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(conta);
        repoContas.Setup(r => r.UpdateAsync(It.IsAny<ContaFinanceira>())).ReturnsAsync((ContaFinanceira c) => c);
        uow.Setup(u => u.ContasFinanceiras).Returns(repoContas.Object);
        uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var svc = new FinanceiroService(uow.Object);
        var dataPagamento = DateTime.Today;

        var result = await svc.QuitarAsync(2, dataPagamento, 310m, FormaPagamentoTipo.Debito, juros: 10m);

        result.Sucesso.Should().BeTrue();
        conta.Status.Should().Be(StatusConta.Paga);
        conta.ValorPago.Should().Be(310m);
        conta.Juros.Should().Be(10m);
        conta.DataPagamento.Should().Be(dataPagamento);
        conta.FormaPagamento.Should().Be(FormaPagamentoTipo.Debito);
    }

    [Fact]
    public async Task QuitarAsync_ComMultaEDesconto_GravaCorretamente()
    {
        var conta = new ContaFinanceira
        {
            Id = 3,
            Status = StatusConta.Atrasada,
            Descricao = "Aluguel",
            Valor = 1000m,
            DataVencimento = DateTime.Today.AddDays(-5)
        };

        var uow = new Mock<IUnitOfWork>();
        var repoContas = new Mock<IRepository<ContaFinanceira>>();
        repoContas.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(conta);
        repoContas.Setup(r => r.UpdateAsync(It.IsAny<ContaFinanceira>())).ReturnsAsync((ContaFinanceira c) => c);
        uow.Setup(u => u.ContasFinanceiras).Returns(repoContas.Object);
        uow.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var svc = new FinanceiroService(uow.Object);

        var result = await svc.QuitarAsync(3, DateTime.Today, 1020m, FormaPagamentoTipo.Boleto,
            juros: 5m, multa: 20m, desconto: 5m);

        result.Sucesso.Should().BeTrue();
        conta.Multa.Should().Be(20m);
        conta.Desconto.Should().Be(5m);
        conta.ValorPago.Should().Be(1020m);
    }
}
