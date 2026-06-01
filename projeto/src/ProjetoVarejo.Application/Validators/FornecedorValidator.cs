using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class FornecedorValidator : AbstractValidator<Fornecedor>
{
    public FornecedorValidator()
    {
        RuleFor(x => x.RazaoSocial)
            .NotEmpty().WithMessage("Razão Social é obrigatória")
            .MinimumLength(3).WithMessage("Razão Social deve ter no mínimo 3 caracteres")
            .MaximumLength(150).WithMessage("Razão Social não pode ter mais de 150 caracteres");

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ é obrigatório")
            .Must(cnpj => CpfCnpjValidator.ValidarCnpj(cnpj))
            .WithMessage("CNPJ inválido");

        RuleFor(x => x.NomeFantasia)
            .MinimumLength(3).WithMessage("Nome Fantasia deve ter no mínimo 3 caracteres")
            .MaximumLength(150).WithMessage("Nome Fantasia não pode ter mais de 150 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.NomeFantasia));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email inválido")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Telefone)
            .MinimumLength(10).WithMessage("Telefone deve ter no mínimo 10 dígitos")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefone));

        RuleFor(x => x.Logradouro)
            .MinimumLength(3).WithMessage("Logradouro deve ter no mínimo 3 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Logradouro));

        RuleFor(x => x.Numero)
            .NotEmpty().WithMessage("Número é obrigatório")
            .When(x => !string.IsNullOrWhiteSpace(x.Logradouro));
    }
}
