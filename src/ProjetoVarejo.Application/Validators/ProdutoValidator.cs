using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class ProdutoValidator : AbstractValidator<Produto>
{
    public ProdutoValidator()
    {
        RuleFor(p => p.Codigo)
            .NotEmpty().WithMessage("Código é obrigatório")
            .Length(1, 50).WithMessage("Código deve ter entre 1 e 50 caracteres")
            .Matches(@"^[A-Za-z0-9\-_]+$").WithMessage("Código deve conter apenas letras, números, hífen e underscore");

        RuleFor(p => p.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .Length(3, 255).WithMessage("Descrição deve ter entre 3 e 255 caracteres");

        RuleFor(p => p.PrecoVenda)
            .GreaterThan(0).WithMessage("Preço de venda deve ser maior que zero");

        RuleFor(p => p.PrecoCusto)
            .GreaterThanOrEqualTo(0).WithMessage("Preço de custo deve ser maior ou igual a zero");

        RuleFor(p => p.Ncm)
            .NotEmpty().WithMessage("NCM é obrigatório para notas fiscais")
            .Length(8).WithMessage("NCM deve ter exatamente 8 dígitos")
            .Matches(@"^\d{8}$").WithMessage("NCM deve conter apenas dígitos")
            .When(p => !string.IsNullOrEmpty(p.Ncm));

        RuleFor(p => p.CodigoBarras)
            .Matches(@"^\d+$").WithMessage("Código de barras deve conter apenas dígitos")
            .When(p => !string.IsNullOrEmpty(p.CodigoBarras));

        RuleFor(p => p.EstoqueMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("Estoque mínimo deve ser maior ou igual a zero");

        RuleFor(p => p.CategoriaId)
            .GreaterThan(0).WithMessage("Categoria é obrigatória")
            .When(p => p.CategoriaId > 0);
    }
}
