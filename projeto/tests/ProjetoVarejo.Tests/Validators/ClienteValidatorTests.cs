using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class ClienteValidatorTests
{
    private readonly ClienteValidator _validator = new();

    [Fact]
    public void Validate_WithValidCliente_ReturnsSuccess()
    {
        // Arrange
        var cliente = new Cliente
        {
            Nome = "João Silva",
            CpfCnpj = "12345678909",
            Email = "joao@example.com",
            Telefone = "1133334444",
            Cep = "12345-678",
            Logradouro = "Rua Exemplo",
            Uf = "SP"
        };

        // Act
        var result = _validator.Validate(cliente);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyNome_ReturnsFail()
    {
        // Arrange
        var cliente = new Cliente { Nome = "" };

        // Act
        var result = _validator.Validate(cliente);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Nome" && e.ErrorMessage.Contains("obrigatório"));
    }

    [Fact]
    public void Validate_WithNomeTooShort_ReturnsFail()
    {
        // Arrange
        var cliente = new Cliente { Nome = "Jo" };

        // Act
        var result = _validator.Validate(cliente);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Nome" && e.ErrorMessage.Contains("mínimo"));
    }

    [Fact]
    public void Validate_WithInvalidCpf_ReturnsFail()
    {
        // Arrange
        var cliente = new Cliente
        {
            Nome = "João Silva",
            CpfCnpj = "00000000000"
        };

        // Act
        var result = _validator.Validate(cliente);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CpfCnpj" && e.ErrorMessage.Contains("inválido"));
    }

    [Fact]
    public void Validate_WithInvalidEmail_ReturnsFail()
    {
        // Arrange
        var cliente = new Cliente
        {
            Nome = "João Silva",
            CpfCnpj = "12345678909",
            Email = "invalid-email"
        };

        // Act
        var result = _validator.Validate(cliente);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email" && e.ErrorMessage.Contains("inválido"));
    }

    [Fact]
    public void Validate_WithValidEmailOrEmpty_ReturnsSuccess()
    {
        // Arrange
        var cliente = new Cliente
        {
            Nome = "João Silva",
            CpfCnpj = "12345678909",
            Email = ""
        };

        // Act
        var result = _validator.Validate(cliente);

        // Assert
        Assert.True(result.IsValid);
    }
}
