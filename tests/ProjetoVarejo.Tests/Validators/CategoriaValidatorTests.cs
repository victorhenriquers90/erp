using FluentValidation.TestHelper;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class CategoriaValidatorTests
{
    private readonly CategoriaValidator _validator;

    public CategoriaValidatorTests()
    {
        _validator = new CategoriaValidator();
    }

    [Fact]
    public void Validate_CategoriaValida_SemErros()
    {
        // Arrange
        var categoria = new Categoria
        {
            Nome = "Alimentos",
            Descricao = "Produtos alimentícios"
        };

        // Act
        var result = _validator.TestValidate(categoria);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NomeVazio_ComErro()
    {
        // Arrange
        var categoria = new Categoria
        {
            Nome = ""
        };

        // Act & Assert
        var result = _validator.TestValidate(categoria);
        result.ShouldHaveValidationErrorFor(c => c.Nome);
    }

    [Fact]
    public void Validate_NomeLongo_ComErro()
    {
        // Arrange
        var categoria = new Categoria
        {
            Nome = new string('A', 101)
        };

        // Act & Assert
        var result = _validator.TestValidate(categoria);
        result.ShouldHaveValidationErrorFor(c => c.Nome);
    }

    [Fact]
    public void Validate_DescricaoLonga_ComErro()
    {
        // Arrange
        var categoria = new Categoria
        {
            Nome = "Alimentos",
            Descricao = new string('A', 501)
        };

        // Act & Assert
        var result = _validator.TestValidate(categoria);
        result.ShouldHaveValidationErrorFor(c => c.Descricao);
    }

    [Fact]
    public void Validate_DescricaoValida_SemErro()
    {
        // Arrange
        var categoria = new Categoria
        {
            Nome = "Alimentos",
            Descricao = new string('A', 500)
        };

        // Act
        var result = _validator.TestValidate(categoria);

        // Assert
        result.ShouldNotHaveValidationErrorFor(c => c.Descricao);
    }
}
