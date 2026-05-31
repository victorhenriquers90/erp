using FluentValidation.TestHelper;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class PagamentoVendaValidatorTests
{
    private readonly PagamentoVendaValidator _validator;

    public PagamentoVendaValidatorTests()
    {
        _validator = new PagamentoVendaValidator();
    }

    [Fact]
    public void Validate_PagamentoDinheiro_SemErros()
    {
        // Arrange
        var pagamento = new PagamentoVenda
        {
            FormaPagamento = FormaPagamentoTipo.Dinheiro,
            Valor = 100
        };

        // Act
        var result = _validator.TestValidate(pagamento);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_PagamentoValorZero_ComErro()
    {
        // Arrange
        var pagamento = new PagamentoVenda
        {
            FormaPagamento = FormaPagamentoTipo.Dinheiro,
            Valor = 0
        };

        // Act & Assert
        var result = _validator.TestValidate(pagamento);
        result.ShouldHaveValidationErrorFor(p => p.Valor);
    }

    [Fact]
    public void Validate_PagamentoCreditoSemNumeroCartao_ComErro()
    {
        // Arrange
        var pagamento = new PagamentoVenda
        {
            FormaPagamento = FormaPagamentoTipo.Credito,
            Valor = 100,
            NumeroCartao = null
        };

        // Act & Assert
        var result = _validator.TestValidate(pagamento);
        result.ShouldHaveValidationErrorFor(p => p.NumeroCartao);
    }

    [Fact]
    public void Validate_PagamentoCreditoComNumeroCartaoValido_SemErro()
    {
        // Arrange
        var pagamento = new PagamentoVenda
        {
            FormaPagamento = FormaPagamentoTipo.Credito,
            Valor = 100,
            NumeroCartao = "1234567890123456",
            Parcelas = 1
        };

        // Act
        var result = _validator.TestValidate(pagamento);

        // Assert
        result.ShouldNotHaveValidationErrorFor(p => p.NumeroCartao);
    }

    [Fact]
    public void Validate_PagamentoCreditoSemParcelas_ComErro()
    {
        // Arrange
        var pagamento = new PagamentoVenda
        {
            FormaPagamento = FormaPagamentoTipo.Credito,
            Valor = 100,
            NumeroCartao = "1234567890123456",
            Parcelas = 0
        };

        // Act & Assert
        var result = _validator.TestValidate(pagamento);
        result.ShouldHaveValidationErrorFor(p => p.Parcelas);
    }

    [Fact]
    public void Validate_PagamentoDebitoComCartao_SemErro()
    {
        // Arrange
        var pagamento = new PagamentoVenda
        {
            FormaPagamento = FormaPagamentoTipo.Debito,
            Valor = 100,
            NumeroCartao = "1234567890123456"
        };

        // Act
        var result = _validator.TestValidate(pagamento);

        // Assert
        result.ShouldNotHaveValidationErrorFor(p => p.NumeroCartao);
    }
}
