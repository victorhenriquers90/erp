using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class ProdutoValidatorTests
{
    private readonly ProdutoValidator _validator = new();

    [Fact]
    public void Validate_WithValidProduto_ReturnsSuccess()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "PROD001",
            Descricao = "Produto de Exemplo",
            PrecoVenda = 100m,
            PrecoCusto = 50m,
            Estoque = 10m,
            EstoqueMinimo = 5m,
            Unidade = UnidadeMedida.UN
        };

        // Act
        var result = _validator.Validate(produto);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyDescricao_ReturnsFail()
    {
        // Arrange
        var produto = new Produto { Descricao = "" };

        // Act
        var result = _validator.Validate(produto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Descricao");
    }

    [Fact]
    public void Validate_WithZeroPrecoVenda_ReturnsFail()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "PROD001",
            Descricao = "Produto",
            PrecoVenda = 0m
        };

        // Act
        var result = _validator.Validate(produto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PrecoVenda");
    }

    [Fact]
    public void Validate_WithPrecoCustoGreaterThanVenda_ReturnsFail()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "PROD001",
            Descricao = "Produto",
            PrecoVenda = 50m,
            PrecoCusto = 100m
        };

        // Act
        var result = _validator.Validate(produto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PrecoVenda");
    }

    [Fact]
    public void Validate_WithNegativeEstoque_ReturnsFail()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "PROD001",
            Descricao = "Produto",
            PrecoVenda = 100m,
            PrecoCusto = 50m,
            Estoque = -1m
        };

        // Act
        var result = _validator.Validate(produto);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Estoque");
    }
}
