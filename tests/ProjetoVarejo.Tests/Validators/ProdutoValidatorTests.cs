using FluentValidation.TestHelper;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class ProdutoValidatorTests
{
    private readonly ProdutoValidator _validator;

    public ProdutoValidatorTests()
    {
        _validator = new ProdutoValidator();
    }

    [Fact]
    public void Validate_ProdutoValido_SemErros()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "P001",
            Descricao = "Produto Teste",
            PrecoVenda = 10.50m,
            Ativo = true
        };

        // Act
        var result = _validator.TestValidate(produto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_PrecoVendaZero_ComErro()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "P001",
            Descricao = "Produto",
            PrecoVenda = 0
        };

        // Act & Assert
        var result = _validator.TestValidate(produto);
        result.ShouldHaveValidationErrorFor(p => p.PrecoVenda);
    }

    [Fact]
    public void Validate_NcmInvalido_ComErro()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "P001",
            Descricao = "Produto",
            PrecoVenda = 10,
            Ncm = "1234567" // 7 dígitos ao invés de 8
        };

        // Act & Assert
        var result = _validator.TestValidate(produto);
        result.ShouldHaveValidationErrorFor(p => p.Ncm);
    }

    [Fact]
    public void Validate_NcmValido_SemErro()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "P001",
            Descricao = "Produto",
            PrecoVenda = 10,
            Ncm = "12345678"
        };

        // Act
        var result = _validator.TestValidate(produto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(p => p.Ncm);
    }

    [Fact]
    public void Validate_CodigoBarrasComLetras_ComErro()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "P001",
            Descricao = "Produto",
            PrecoVenda = 10,
            CodigoBarras = "789ABC0000001"
        };

        // Act & Assert
        var result = _validator.TestValidate(produto);
        result.ShouldHaveValidationErrorFor(p => p.CodigoBarras);
    }

    [Fact]
    public void Validate_CodigoBarrasValido_SemErro()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "P001",
            Descricao = "Produto",
            PrecoVenda = 10,
            CodigoBarras = "7890000000001"
        };

        // Act
        var result = _validator.TestValidate(produto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(p => p.CodigoBarras);
    }

    [Fact]
    public void Validate_EstoqueNegativo_ComErro()
    {
        // Arrange
        var produto = new Produto
        {
            Codigo = "P001",
            Descricao = "Produto",
            PrecoVenda = 10,
            EstoqueMinimo = -5
        };

        // Act & Assert
        var result = _validator.TestValidate(produto);
        result.ShouldHaveValidationErrorFor(p => p.EstoqueMinimo);
    }
}
