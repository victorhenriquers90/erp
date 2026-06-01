using ProjetoVarejo.Application.Validators;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class CpfCnpjValidatorTests
{
    #region CPF Tests

    [Theory]
    [InlineData("123.456.789-09")] // Valid CPF format
    [InlineData("12345678909")]      // Valid CPF unformatted
    public void ValidarCpf_WithValidCpf_ReturnsTrue(string cpf)
    {
        // Arrange & Act
        var result = CpfCnpjValidator.ValidarCpf(cpf);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    [InlineData("11111111111")] // All same digits
    [InlineData("123.456.789-00")] // Invalid check digit
    [InlineData("12345678901")] // Wrong length
    public void ValidarCpf_WithInvalidCpf_ReturnsFalse(string cpf)
    {
        // Act
        var result = CpfCnpjValidator.ValidarCpf(cpf);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CNPJ Tests

    [Theory]
    [InlineData("11.222.333/0001-81")] // Valid CNPJ format
    [InlineData("11222333000181")]       // Valid CNPJ unformatted
    public void ValidarCnpj_WithValidCnpj_ReturnsTrue(string cnpj)
    {
        // Arrange & Act
        var result = CpfCnpjValidator.ValidarCnpj(cnpj);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData(null)] // Null
    [InlineData("11111111111111")] // All same digits
    [InlineData("11.222.333/0001-80")] // Invalid check digit
    [InlineData("1122233300018")] // Wrong length
    public void ValidarCnpj_WithInvalidCnpj_ReturnsFalse(string cnpj)
    {
        // Act
        var result = CpfCnpjValidator.ValidarCnpj(cnpj);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region CPF or CNPJ Tests

    [Theory]
    [InlineData("123.456.789-09")] // Valid CPF
    [InlineData("11.222.333/0001-81")] // Valid CNPJ
    public void ValidarCpfOuCnpj_WithValidDocument_ReturnsTrue(string documento)
    {
        // Act
        var result = CpfCnpjValidator.ValidarCpfOuCnpj(documento);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("")] // Empty
    [InlineData("123")] // Too short
    [InlineData("11111111111")] // Invalid CPF (same digits)
    [InlineData("11111111111111")] // Invalid CNPJ (same digits)
    public void ValidarCpfOuCnpj_WithInvalidDocument_ReturnsFalse(string documento)
    {
        // Act
        var result = CpfCnpjValidator.ValidarCpfOuCnpj(documento);

        // Assert
        Assert.False(result);
    }

    #endregion
}
