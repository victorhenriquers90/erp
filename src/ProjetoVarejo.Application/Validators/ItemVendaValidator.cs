using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class ItemVendaValidator : AbstractValidator<ItemVenda>
{
    public ItemVendaValidator()
    {
        RuleFor(i => i.ProdutoId)
            .GreaterThan(0).WithMessage("Produto é obrigatório");

        RuleFor(i => i.Quantidade)
            .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero")
            .NotNull().WithMessage("Quantidade é obrigatória");

        RuleFor(i => i.PrecoUnitario)
            .GreaterThan(0).WithMessage("Valor unitário deve ser maior que zero");

        RuleFor(i => i.Desconto)
            .GreaterThanOrEqualTo(0).WithMessage("Desconto não pode ser negativo")
            .LessThanOrEqualTo(i => i.PrecoUnitario * i.Quantidade)
            .WithMessage("Desconto não pode ser maior que o valor do item");
    }
}
