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
            // Must() avalia DateTime.Now em tempo de execução (não no construtor).
            // LessThanOrEqualTo(DateTime.Now) captura o valor UMA VEZ ao construir o
            // validador, então AbertaEm — definido logo depois — sempre fica "no futuro".
            .Must(d => d <= DateTime.Now.AddMinutes(1))
            .WithMessage("Data de abertura não pode ser no futuro");

        RuleFor(c => c.ValorFechamentoInformado)
            .GreaterThanOrEqualTo(0).WithMessage("Valor de fechamento informado não pode ser negativo")
            .When(c => c.FechadaEm.HasValue);

        RuleFor(c => c.ValorFechamentoCalculado)
            .GreaterThanOrEqualTo(0).WithMessage("Valor de fechamento calculado não pode ser negativo")
            .When(c => c.FechadaEm.HasValue);
    }
}
