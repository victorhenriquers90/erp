using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class CategoriaValidator : AbstractValidator<Categoria>
{
    public CategoriaValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome da categoria é obrigatório")
            .Length(1, 100).WithMessage("Nome da categoria deve ter entre 1 e 100 caracteres");

        RuleFor(c => c.Descricao)
            .Length(0, 500).WithMessage("Descrição deve ter no máximo 500 caracteres")
            .When(c => !string.IsNullOrEmpty(c.Descricao));
    }
}
