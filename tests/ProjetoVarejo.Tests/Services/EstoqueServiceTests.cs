using Moq;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using ProjetoVarejo.Tests.Builders;
using Xunit;

namespace ProjetoVarejo.Tests.Services;

/// <summary>
/// Comprehensive unit tests for EstoqueService using MockUnitOfWorkFactory.
/// Tests inventory movement recording, movement listing, and low stock alerts.
/// </summary>
public class EstoqueServiceTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly SessaoApp _sessao;
    private readonly EstoqueService _estoqueService;

    public EstoqueServiceTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _sessao = new SessaoApp();
        _estoqueService = new EstoqueService(_mockUnitOfWork.Object, _sessao);
    }

    #region RegistrarMovimentoAsync Tests

    [Fact]
    public async Task RegistrarMovimentoAsync_EntradaMovimento_CreatesSuccessfully()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin(1);
        var produtoId = 1;
        var quantidade = 10m;
        var custoUnitario = 50m;
        var documento = "NF123";
        var observacao = "Entrada de estoque";

        _sessao.DefinirUsuario(usuario);

        var mockMovimentos = new Mock<IRepository<MovimentoEstoque>>();
        mockMovimentos.Setup(r => r.InsertAsync(It.IsAny<MovimentoEstoque>()))
            .Returns((MovimentoEstoque m) => Task.FromResult(m));

        var mockProdutos = new Mock<IRepository<Produto>>();
        var produto = new ProdutoBuilder().WithId(produtoId).WithEstoque(5m).Build();
        mockProdutos.Setup(r => r.GetByIdAsync(produtoId)).ReturnsAsync(produto);

        _mockUnitOfWork.Setup(u => u.MovimentosEstoque).Returns(mockMovimentos.Object);
        _mockUnitOfWork.Setup(u => u.Produtos).Returns(mockProdutos.Object);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var resultado = await _estoqueService.RegistrarMovimentoAsync(
            produtoId,
            TipoMovimentoEstoque.Entrada,
            quantidade,
            custoUnitario,
            documento,
            null,
            null,
            observacao);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.NotNull(resultado.Valor);
        Assert.Equal(TipoMovimentoEstoque.Entrada, resultado.Valor.Tipo);
        mockMovimentos.Verify(r => r.InsertAsync(It.IsAny<MovimentoEstoque>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegistrarMovimentoAsync_SaidaMovimento_CreatesSuccessfully()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateCaixa(2);
        var produtoId = 1;
        var quantidade = 3m;
        var vendaId = 100;
        var observacao = "Saída por venda";

        _sessao.DefinirUsuario(usuario);

        var mockMovimentos = new Mock<IRepository<MovimentoEstoque>>();
        mockMovimentos.Setup(r => r.InsertAsync(It.IsAny<MovimentoEstoque>()))
            .Returns((MovimentoEstoque m) => Task.FromResult(m));

        var mockProdutos = new Mock<IRepository<Produto>>();
        var produto = new ProdutoBuilder().WithId(produtoId).WithEstoque(10m).Build();
        mockProdutos.Setup(r => r.GetByIdAsync(produtoId)).ReturnsAsync(produto);

        _mockUnitOfWork.Setup(u => u.MovimentosEstoque).Returns(mockMovimentos.Object);
        _mockUnitOfWork.Setup(u => u.Produtos).Returns(mockProdutos.Object);
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var resultado = await _estoqueService.RegistrarMovimentoAsync(
            produtoId,
            TipoMovimentoEstoque.Saida,
            quantidade,
            null,
            null,
            vendaId,
            null,
            observacao);

        // Assert
        Assert.True(resultado.Sucesso);
        Assert.Equal(quantidade, resultado.Valor!.Quantidade);
        Assert.Equal(vendaId, resultado.Valor.VendaId);
        mockMovimentos.Verify(r => r.InsertAsync(It.IsAny<MovimentoEstoque>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarMovimentoAsync_SaidaExceedsStock_ReturnsFail()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateCaixa(2);
        var produtoId = 1;
        var quantidade = 15m; // Request more than available
        var estoque = 5m;

        _sessao.DefinirUsuario(usuario);

        var mockProdutos = new Mock<IRepository<Produto>>();
        var produto = new ProdutoBuilder().WithId(produtoId).WithEstoque(estoque).WithControlaEstoque(true).Build();
        mockProdutos.Setup(r => r.GetByIdAsync(produtoId)).ReturnsAsync(produto);

        _mockUnitOfWork.Setup(u => u.Produtos).Returns(mockProdutos.Object);

        // Act
        var resultado = await _estoqueService.RegistrarMovimentoAsync(
            produtoId,
            TipoMovimentoEstoque.Saida,
            quantidade,
            null,
            null,
            null,
            null,
            "Saída");

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Contains("insuficiente", resultado.Erro ?? "", StringComparison.OrdinalIgnoreCase);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegistrarMovimentoAsync_ProdutoNaoExiste_ReturnsFail()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin(1);
        _sessao.DefinirUsuario(usuario);

        var mockProdutos = new Mock<IRepository<Produto>>();
        mockProdutos.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Produto?)null);

        _mockUnitOfWork.Setup(u => u.Produtos).Returns(mockProdutos.Object);

        // Act
        var resultado = await _estoqueService.RegistrarMovimentoAsync(
            999,
            TipoMovimentoEstoque.Entrada,
            10m,
            50m,
            "NF123",
            null);

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Contains("não encontrado", resultado.Erro ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarMovimentoAsync_ZeroQuantidade_ReturnsFail()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin(1);
        _sessao.DefinirUsuario(usuario);

        var mockProdutos = new Mock<IRepository<Produto>>();
        var produto = new ProdutoBuilder().WithId(1).Build();
        mockProdutos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(produto);

        _mockUnitOfWork.Setup(u => u.Produtos).Returns(mockProdutos.Object);

        // Act
        var resultado = await _estoqueService.RegistrarMovimentoAsync(
            1,
            TipoMovimentoEstoque.Entrada,
            0m,
            50m,
            "NF123",
            null);

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Contains("maior que zero", resultado.Erro ?? "", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrarMovimentoAsync_NegativeQuantidade_ReturnsFail()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin(1);
        _sessao.DefinirUsuario(usuario);

        var mockProdutos = new Mock<IRepository<Produto>>();
        var produto = new ProdutoBuilder().WithId(1).Build();
        mockProdutos.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(produto);

        _mockUnitOfWork.Setup(u => u.Produtos).Returns(mockProdutos.Object);

        // Act
        var resultado = await _estoqueService.RegistrarMovimentoAsync(
            1,
            TipoMovimentoEstoque.Saida,
            -5m,
            null,
            null,
            null);

        // Assert
        Assert.False(resultado.Sucesso);
    }

    [Fact]
    public async Task RegistrarMovimentoAsync_UnauthenticatedUser_ReturnsFail()
    {
        // Arrange — _sessao.UsuarioLogado is null by default

        // Act
        var resultado = await _estoqueService.RegistrarMovimentoAsync(
            1,
            TipoMovimentoEstoque.Entrada,
            10m,
            50m,
            "NF123",
            null);

        // Assert
        Assert.False(resultado.Sucesso);
        Assert.Contains("não autenticado", resultado.Erro ?? "", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region ListarMovimentosAsync Tests

    [Fact]
    public async Task ListarMovimentosAsync_WithoutFilters_ReturnsAllMovimentos()
    {
        // Arrange
        var movimentos = new List<MovimentoEstoque>
        {
            new() { Id = 1, ProdutoId = 1, Tipo = TipoMovimentoEstoque.Entrada, Quantidade = 10m },
            new() { Id = 2, ProdutoId = 1, Tipo = TipoMovimentoEstoque.Saida, Quantidade = 5m },
            new() { Id = 3, ProdutoId = 2, Tipo = TipoMovimentoEstoque.Entrada, Quantidade = 20m }
        };

        var mockMovimentos = new Mock<IRepository<MovimentoEstoque>>();
        mockMovimentos.Setup(r => r.Query()).Returns(movimentos.AsQueryable());

        _mockUnitOfWork.Setup(u => u.MovimentosEstoque).Returns(mockMovimentos.Object);

        // Act
        var resultado = await _estoqueService.ListarMovimentosAsync();

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(3, resultado.Count);
    }

    [Fact]
    public async Task ListarMovimentosAsync_FilterByProdutoId_ReturnsFilteredResults()
    {
        // Arrange
        var produtoId = 1;
        var movimentos = new List<MovimentoEstoque>
        {
            new() { Id = 1, ProdutoId = 1, Tipo = TipoMovimentoEstoque.Entrada, Quantidade = 10m },
            new() { Id = 2, ProdutoId = 1, Tipo = TipoMovimentoEstoque.Saida, Quantidade = 5m }
        };

        var mockMovimentos = new Mock<IRepository<MovimentoEstoque>>();
        mockMovimentos.Setup(r => r.Query()).Returns(movimentos.Where(m => m.ProdutoId == produtoId).AsQueryable());

        _mockUnitOfWork.Setup(u => u.MovimentosEstoque).Returns(mockMovimentos.Object);

        // Act
        var resultado = await _estoqueService.ListarMovimentosAsync(produtoId: produtoId);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, m => Assert.Equal(produtoId, m.ProdutoId));
    }

    #endregion

    #region ProdutosAbaixoMinimoAsync Tests

    [Fact]
    public async Task ProdutosAbaixoMinimoAsync_WithProductsBelowMinimum_ReturnsLowStockProducts()
    {
        // Arrange
        var produtos = new List<Produto>
        {
            new() { Id = 1, Descricao = "Produto A", Estoque = 5m, EstoqueMinimo = 10m, ControlaEstoque = true, Ativo = true },
            new() { Id = 2, Descricao = "Produto B", Estoque = 20m, EstoqueMinimo = 15m, ControlaEstoque = true, Ativo = true },
            new() { Id = 3, Descricao = "Produto C", Estoque = 3m, EstoqueMinimo = 10m, ControlaEstoque = true, Ativo = true }
        };

        var mockProdutos = new Mock<IRepository<Produto>>();
        mockProdutos.Setup(r => r.Query())
            .Returns(produtos.Where(p => p.Ativo && p.ControlaEstoque && p.Estoque <= p.EstoqueMinimo).AsQueryable());

        _mockUnitOfWork.Setup(u => u.Produtos).Returns(mockProdutos.Object);

        // Act
        var resultado = await _estoqueService.ProdutosAbaixoMinimoAsync();

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(2, resultado.Count);
        Assert.All(resultado, p => Assert.True(p.Estoque <= p.EstoqueMinimo));
    }

    [Fact]
    public async Task ProdutosAbaixoMinimoAsync_NoProductsBelowMinimum_ReturnsEmpty()
    {
        // Arrange
        var produtos = new List<Produto>
        {
            new() { Id = 1, Descricao = "Produto A", Estoque = 100m, EstoqueMinimo = 10m, ControlaEstoque = true, Ativo = true }
        };

        var mockProdutos = new Mock<IRepository<Produto>>();
        mockProdutos.Setup(r => r.Query())
            .Returns(produtos.Where(p => p.Ativo && p.ControlaEstoque && p.Estoque <= p.EstoqueMinimo).AsQueryable());

        _mockUnitOfWork.Setup(u => u.Produtos).Returns(mockProdutos.Object);

        // Act
        var resultado = await _estoqueService.ProdutosAbaixoMinimoAsync();

        // Assert
        Assert.NotNull(resultado);
        Assert.Empty(resultado);
    }

    #endregion
}
