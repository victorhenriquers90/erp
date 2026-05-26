using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class CaixaSessionValidator : AbstractValidator<CaixaSessao>
{
    public CaixaSessionValidator()
    {
        RuleFor(c => c.ValorAbertura)
            .GreaterThanOrEqualTo(0).WithMessage("Valor de abertura não pode ser negativo");

        RuleFor(c => c.UsuarioAberturaId)
            .GreaterThan(0).WithMessage("Usuário de abertura é obrigatório");

        RuleFor(c => c.AbertaEm)
            .NotNull().WithMessage("Data de abertura é obrigatória")
            .LessThanOrEqualTo(DateTime.Now).WithMessage("Data de abertura não pode ser no futuro");

        RuleFor(c => c.ValorFechamentoInformado)
            .GreaterThanOrEqualTo(0).WithMessage("Valor de fechamento informado não pode ser negativo")
            .When(c => c.FechadaEm.HasValue);

        RuleFor(c => c.ValorFechamentoCalculado)
            .GreaterThanOrEqualTo(0).WithMessage("Valor de fechamento calculado não pode ser negativo")
            .When(c => c.FechadaEm.HasValue);
    }
}
