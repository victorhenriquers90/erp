using FluentValidation.TestHelper;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class CaixaSessionValidatorTests
{
    private readonly CaixaSessionValidator _validator;

    public CaixaSessionValidatorTests()
    {
        _validator = new CaixaSessionValidator();
    }

    [Fact]
    public void Validate_CaixaValida_SemErros()
    {
        // Arrange
        var caixa = new CaixaSessao
        {
            ValorAbertura = 100,
            UsuarioAberturaId = 1,
            AbertaEm = DateTime.Now.AddHours(-2)
        };

        // Act
        var result = _validator.TestValidate(caixa);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ValorAberturaNegatico_ComErro()
    {
        // Arrange
        var caixa = new CaixaSessao
        {
            ValorAbertura = -50,
            UsuarioAberturaId = 1,
            AbertaEm = DateTime.Now
        };

        // Act & Assert
        var result = _validator.TestValidate(caixa);
        result.ShouldHaveValidationErrorFor(c => c.ValorAbertura);
    }

    [Fact]
    public void Validate_UsuarioAberturaZero_ComErro()
    {
        // Arrange
        var caixa = new CaixaSessao
        {
            ValorAbertura = 100,
            UsuarioAberturaId = 0,
            AbertaEm = DateTime.Now
        };

        // Act & Assert
        var result = _validator.TestValidate(caixa);
        result.ShouldHaveValidationErrorFor(c => c.UsuarioAberturaId);
    }

    [Fact]
    public void Validate_DataAberturaNoFuturo_ComErro()
    {
        // Arrange
        var caixa = new CaixaSessao
        {
            ValorAbertura = 100,
            UsuarioAberturaId = 1,
            AbertaEm = DateTime.Now.AddDays(1) // Futuro
        };

        // Act & Assert
        var result = _validator.TestValidate(caixa);
        result.ShouldHaveValidationErrorFor(c => c.AbertaEm);
    }

    [Fact]
    public void Validate_ValorFechamentoNegativo_ComErro()
    {
        // Arrange
        var caixa = new CaixaSessao
        {
            ValorAbertura = 100,
            UsuarioAberturaId = 1,
            AbertaEm = DateTime.Now.AddHours(-2),
            FechadaEm = DateTime.Now,
            ValorFechamento = -10
        };

        // Act & Assert
        var result = _validator.TestValidate(caixa);
        result.ShouldHaveValidationErrorFor(c => c.ValorFechamento);
    }

    [Fact]
    public void Validate_ValorFechamentoValido_SemErro()
    {
        // Arrange
        var caixa = new CaixaSessao
        {
            ValorAbertura = 100,
            UsuarioAberturaId = 1,
            AbertaEm = DateTime.Now.AddHours(-2),
            FechadaEm = DateTime.Now,
            ValorFechamento = 150
        };

        // Act
        var result = _validator.TestValidate(caixa);

        // Assert
        result.ShouldNotHaveValidationErrorFor(c => c.ValorFechamento);
    }
}
