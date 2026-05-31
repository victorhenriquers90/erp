using FluentValidation.TestHelper;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using Xunit;

namespace ProjetoVarejo.Tests.Validators;

public class VendaValidatorTests
{
    private readonly VendaValidator _validator;
    private readonly FinalizarVendaValidator _finalizarValidator;

    public VendaValidatorTests()
    {
        _validator = new VendaValidator();
        _finalizarValidator = new FinalizarVendaValidator();
    }

    [Fact]
    public void Validate_VendaValida_SemErros()
    {
        // Arrange
        var venda = new Venda
        {
            UsuarioId = 1,
            Status = StatusVenda.EmAberto,
            Total = 100,
            SubTotal = 100,
            Desconto = 0,
            Acrescimo = 0,
            Itens = new List<ItemVenda>()
        };

        // Act
        var result = _validator.TestValidate(venda);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DescontinuadaVenda_SemUsuario_ComErro()
    {
        // Arrange
        var venda = new Venda
        {
            UsuarioId = 0,
            Status = StatusVenda.EmAberto,
            Total = 100
        };

        // Act & Assert
        var result = _validator.TestValidate(venda);
        result.ShouldHaveValidationErrorFor(v => v.UsuarioId);
    }

    [Fact]
    public void Validate_TotalNegativo_ComErro()
    {
        // Arrange
        var venda = new Venda
        {
            UsuarioId = 1,
            Status = StatusVenda.EmAberto,
            Total = -10,
            SubTotal = 100
        };

        // Act & Assert
        var result = _validator.TestValidate(venda);
        result.ShouldHaveValidationErrorFor(v => v.Total);
    }

    [Fact]
    public void Validate_DescontoMaiorQueSubtotal_ComErro()
    {
        // Arrange
        var venda = new Venda
        {
            UsuarioId = 1,
            Status = StatusVenda.EmAberto,
            Total = 50,
            SubTotal = 100,
            Desconto = 150
        };

        // Act & Assert
        var result = _validator.TestValidate(venda);
        result.ShouldHaveValidationErrorFor(v => v.Desconto);
    }

    [Fact]
    public void Validate_VendaFinalizadaSemItens_ComErro()
    {
        // Arrange
        var venda = new Venda
        {
            UsuarioId = 1,
            Status = StatusVenda.Finalizada,
            Total = 100,
            SubTotal = 100,
            Itens = new List<ItemVenda>()
        };

        // Act & Assert
        var result = _validator.TestValidate(venda);
        result.ShouldHaveValidationErrorFor(v => v.Itens);
    }

    [Fact]
    public void Validate_FinalizarVendaComPagamento_SemErros()
    {
        // Arrange
        var venda = new Venda
        {
            Id = 1,
            UsuarioId = 1,
            Status = StatusVenda.EmAberto,
            Total = 100,
            SubTotal = 100,
            Desconto = 0,
            Acrescimo = 0,
            Itens = new List<ItemVenda>
            {
                new ItemVenda { ProdutoId = 1, Quantidade = 1, ValorUnitario = 100 }
            }
        };

        var pagamentos = new List<PagamentoVenda>
        {
            new PagamentoVenda { FormaPagamento = FormaPagamentoTipo.Dinheiro, Valor = 100 }
        };

        // Act
        var result = _finalizarValidator.TestValidate((venda, pagamentos));

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_FinalizarVendaComValorInsuficiente_ComErro()
    {
        // Arrange
        var venda = new Venda
        {
            Id = 1,
            UsuarioId = 1,
            Status = StatusVenda.EmAberto,
            Total = 100,
            SubTotal = 100
        };

        var pagamentos = new List<PagamentoVenda>
        {
            new PagamentoVenda { FormaPagamento = FormaPagamentoTipo.Dinheiro, Valor = 50 }
        };

        // Act & Assert
        var result = _finalizarValidator.TestValidate((venda, pagamentos));
        result.ShouldHaveValidationErrorFor(x => x.pagamentos);
    }
}
