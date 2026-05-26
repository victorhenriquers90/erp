using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class NotaFiscalValidator : AbstractValidator<NotaFiscal>
{
    public NotaFiscalValidator()
    {
        RuleFor(n => n.Numero)
            .GreaterThan(0).WithMessage("Número da NF-e deve ser maior que zero");

        RuleFor(n => n.Serie)
            .GreaterThan(0).WithMessage("Série da NF-e deve ser maior que zero");

        RuleFor(n => n.Modelo)
            .NotEmpty().WithMessage("Modelo é obrigatório")
            .Length(2).WithMessage("Modelo deve ter exatamente 2 caracteres");

        RuleFor(n => n.ChaveAcesso)
            .NotEmpty().WithMessage("Chave de acesso é obrigatória")
            .Length(44).WithMessage("Chave de acesso deve ter exatamente 44 caracteres")
            .Matches(@"^\d{44}$").WithMessage("Chave de acesso deve conter apenas dígitos");

        RuleFor(n => n.VendaId)
            .GreaterThan(0).WithMessage("Venda é obrigatória");

        RuleFor(n => n.Status)
            .IsInEnum().WithMessage("Status da NF-e inválido");
    }
}
