using FluentValidation.TestHelper;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class ItemVendaValidatorTests
{
    private readonly ItemVendaValidator _validator;

    public ItemVendaValidatorTests()
    {
        _validator = new ItemVendaValidator();
    }

    [Fact]
    public void Validate_ItemValido_SemErros()
    {
        // Arrange
        var item = new ItemVenda
        {
            ProdutoId = 1,
            Quantidade = 5,
            PrecoUnitario = 10.50m,
            Desconto = 0
        };

        // Act
        var result = _validator.TestValidate(item);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_QuantidadeZero_ComErro()
    {
        // Arrange
        var item = new ItemVenda
        {
            ProdutoId = 1,
            Quantidade = 0,
            PrecoUnitario = 10.50m
        };

        // Act & Assert
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(i => i.Quantidade);
    }

    [Fact]
    public void Validate_PrecoUnitarioZero_ComErro()
    {
        // Arrange
        var item = new ItemVenda
        {
            ProdutoId = 1,
            Quantidade = 5,
            PrecoUnitario = 0
        };

        // Act & Assert
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(i => i.PrecoUnitario);
    }

    [Fact]
    public void Validate_DescontoMaiorQueValor_ComErro()
    {
        // Arrange
        var item = new ItemVenda
        {
            ProdutoId = 1,
            Quantidade = 5,
            PrecoUnitario = 10,
            Desconto = 60 // 5 * 10 = 50, desconto = 60 > 50
        };

        // Act & Assert
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(i => i.Desconto);
    }

    [Fact]
    public void Validate_DescontoValido_SemErro()
    {
        // Arrange
        var item = new ItemVenda
        {
            ProdutoId = 1,
            Quantidade = 5,
            PrecoUnitario = 10,
            Desconto = 20 // 5 * 10 = 50, desconto = 20 <= 50
        };

        // Act
        var result = _validator.TestValidate(item);

        // Assert
        result.ShouldNotHaveValidationErrorFor(i => i.Desconto);
    }

    [Fact]
    public void Validate_ProdutoIdZero_ComErro()
    {
        // Arrange
        var item = new ItemVenda
        {
            ProdutoId = 0,
            Quantidade = 5,
            PrecoUnitario = 10
        };

        // Act & Assert
        var result = _validator.TestValidate(item);
        result.ShouldHaveValidationErrorFor(i => i.ProdutoId);
    }
}
