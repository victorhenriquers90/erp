using FluentValidation.TestHelper;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class UsuarioValidatorTests
{
    private readonly UsuarioValidator _validator;

    public UsuarioValidatorTests()
    {
        _validator = new UsuarioValidator();
    }

    [Fact]
    public void Validate_UsuarioValido_SemErros()
    {
        // Arrange
        var usuario = new Usuario
        {
            Login = "usuario123",
            Nome = "João Silva",
            Perfil = PerfilUsuario.Caixa,
            Ativo = true,
            SenhaHash = "hash_qualquer"
        };

        // Act
        var result = _validator.TestValidate(usuario);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_LoginVazio_ComErro()
    {
        // Arrange
        var usuario = new Usuario { Login = "", Nome = "João", Perfil = PerfilUsuario.Caixa };

        // Act & Assert
        var result = _validator.TestValidate(usuario);
        result.ShouldHaveValidationErrorFor(u => u.Login);
    }

    [Fact]
    public void Validate_LoginMaiusculo_ComErro()
    {
        // Arrange
        var usuario = new Usuario { Login = "USUARIO", Nome = "João", Perfil = PerfilUsuario.Caixa };

        // Act & Assert
        var result = _validator.TestValidate(usuario);
        result.ShouldHaveValidationErrorFor(u => u.Login)
            .WithErrorMessage("Login deve ser em minúsculas");
    }

    [Fact]
    public void Validate_NomeComCaracteresEspeciais_ComErro()
    {
        // Arrange
        var usuario = new Usuario { Login = "usuario", Nome = "João@Silva", Perfil = PerfilUsuario.Caixa };

        // Act & Assert
        var result = _validator.TestValidate(usuario);
        result.ShouldHaveValidationErrorFor(u => u.Nome);
    }

    [Fact]
    public void Validate_SenhaFraca_ComErro()
    {
        // Arrange
        var validator = new CriarUsuarioValidator();
        var data = ("usuario", "João Silva", PerfilUsuario.Caixa, "123456");

        // Act & Assert
        var result = validator.TestValidate(data);
        result.ShouldHaveValidationErrorFor(x => x.Item4);
    }

    [Fact]
    public void Validate_SenhaForte_SemErro()
    {
        // Arrange
        var validator = new CriarUsuarioValidator();
        var data = ("usuario", "João Silva", PerfilUsuario.Caixa, "Senha123");

        // Act
        var result = validator.TestValidate(data);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Item4);
    }

    [Fact]
    public void Validate_LoginComCaracteresEspeciais_ComErro()
    {
        // Arrange
        var validator = new CriarUsuarioValidator();
        var data = ("user@name", "João Silva", PerfilUsuario.Caixa, "Senha123");

        // Act & Assert
        var result = validator.TestValidate(data);
        result.ShouldHaveValidationErrorFor(x => x.Item1);
    }
}
