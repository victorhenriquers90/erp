using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class ProdutoValidator : AbstractValidator<Produto>
{
    public ProdutoValidator()
    {
        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .MinimumLength(3).WithMessage("Descrição deve ter no mínimo 3 caracteres")
            .MaximumLength(200).WithMessage("Descrição não pode ter mais de 200 caracteres");

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("Código é obrigatório")
            .MinimumLength(3).WithMessage("Código deve ter no mínimo 3 caracteres")
            .MaximumLength(50).WithMessage("Código não pode ter mais de 50 caracteres");

        RuleFor(x => x.PrecoVenda)
            .GreaterThan(0).WithMessage("Preço de venda deve ser maior que 0");

        RuleFor(x => x.PrecoCusto)
            .GreaterThan(0).WithMessage("Preço de custo deve ser maior que 0");

        RuleFor(x => x.PrecoVenda)
            .GreaterThan(x => x.PrecoCusto)
            .WithMessage("Preço de venda deve ser maior que o preço de custo")
            .When(x => x.PrecoCusto > 0);

        RuleFor(x => x.Estoque)
            .GreaterThanOrEqualTo(0).WithMessage("Estoque não pode ser negativo");

        RuleFor(x => x.EstoqueMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("Estoque mínimo não pode ser negativo");
    }
}
