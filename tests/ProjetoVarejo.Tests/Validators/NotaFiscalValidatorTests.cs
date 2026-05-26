using FluentValidation.TestHelper;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class NotaFiscalValidatorTests
{
    private readonly NotaFiscalValidator _validator;

    public NotaFiscalValidatorTests()
    {
        _validator = new NotaFiscalValidator();
    }

    [Fact]
    public void Validate_NotaFiscalValida_SemErros()
    {
        // Arrange
        var nota = new NotaFiscal
        {
            Numero = 100,
            Serie = 1,
            Modelo = "65",
            VendaId = 1,
            ChaveAcesso = "12345678901234567890123456789012345678901234",
            Status = StatusNotaFiscal.Autorizada
        };

        // Act
        var result = _validator.TestValidate(nota);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_NumeroZero_ComErro()
    {
        // Arrange
        var nota = new NotaFiscal
        {
            Numero = 0,
            Serie = 1,
            Modelo = "65",
            VendaId = 1,
            ChaveAcesso = "12345678901234567890123456789012345678901234"
        };

        // Act & Assert
        var result = _validator.TestValidate(nota);
        result.ShouldHaveValidationErrorFor(n => n.Numero);
    }

    [Fact]
    public void Validate_SerieZero_ComErro()
    {
        // Arrange
        var nota = new NotaFiscal
        {
            Numero = 100,
            Serie = 0,
            Modelo = "65",
            VendaId = 1,
            ChaveAcesso = "12345678901234567890123456789012345678901234"
        };

        // Act & Assert
        var result = _validator.TestValidate(nota);
        result.ShouldHaveValidationErrorFor(n => n.Serie);
    }

    [Fact]
    public void Validate_ModeloComTresCaracteres_ComErro()
    {
        // Arrange
        var nota = new NotaFiscal
        {
            Numero = 100,
            Serie = 1,
            Modelo = "650",
            VendaId = 1,
            ChaveAcesso = "12345678901234567890123456789012345678901234"
        };

        // Act & Assert
        var result = _validator.TestValidate(nota);
        result.ShouldHaveValidationErrorFor(n => n.Modelo);
    }

    [Fact]
    public void Validate_ChaveAcessoComLetras_ComErro()
    {
        // Arrange
        var nota = new NotaFiscal
        {
            Numero = 100,
            Serie = 1,
            Modelo = "65",
            VendaId = 1,
            ChaveAcesso = "1234567890123456789012345678901234567890ABCD"
        };

        // Act & Assert
        var result = _validator.TestValidate(nota);
        result.ShouldHaveValidationErrorFor(n => n.ChaveAcesso);
    }

    [Fact]
    public void Validate_ChaveAcessoCurta_ComErro()
    {
        // Arrange
        var nota = new NotaFiscal
        {
            Numero = 100,
            Serie = 1,
            Modelo = "65",
            VendaId = 1,
            ChaveAcesso = "123456789012345678901234567890123456789012"
        };

        // Act & Assert
        var result = _validator.TestValidate(nota);
        result.ShouldHaveValidationErrorFor(n => n.ChaveAcesso);
    }

    [Fact]
    public void Validate_VendaIdZero_ComErro()
    {
        // Arrange
        var nota = new NotaFiscal
        {
            Numero = 100,
            Serie = 1,
            Modelo = "65",
            VendaId = 0,
            ChaveAcesso = "12345678901234567890123456789012345678901234"
        };

        // Act & Assert
        var result = _validator.TestValidate(nota);
        result.ShouldHaveValidationErrorFor(n => n.VendaId);
    }
}
