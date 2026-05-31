using FluentValidation;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Validators;

public class PagamentoVendaValidator : AbstractValidator<PagamentoVenda>
{
    public PagamentoVendaValidator()
    {
        RuleFor(p => p.FormaPagamento)
            .IsInEnum().WithMessage("Forma de pagamento inválida");

        RuleFor(p => p.Valor)
            .GreaterThan(0).WithMessage("Valor de pagamento deve ser maior que zero");

        RuleFor(p => p.Parcelas)
            .GreaterThan(0).WithMessage("Número de parcelas deve ser maior que zero")
            .When(p => p.FormaPagamento == FormaPagamentoTipo.Credito);
    }
}
