using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class ClienteValidator : AbstractValidator<Cliente>
{
    public ClienteValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .MinimumLength(3).WithMessage("Nome deve ter no mínimo 3 caracteres")
            .MaximumLength(150).WithMessage("Nome não pode ter mais de 150 caracteres");

        RuleFor(x => x.CpfCnpj)
            .NotEmpty().WithMessage("CPF/CNPJ é obrigatório")
            .Must((documento) => string.IsNullOrWhiteSpace(documento) || CpfCnpjValidator.ValidarCpfOuCnpj(documento))
            .WithMessage("CPF/CNPJ inválido");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Email inválido")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Telefone)
            .MinimumLength(10).WithMessage("Telefone deve ter no mínimo 10 dígitos")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefone));

        RuleFor(x => x.Cep)
            .Matches(@"^\d{5}-\d{3}$|^\d{8}$").WithMessage("CEP deve estar no formato 12345-678 ou 12345678")
            .When(x => !string.IsNullOrWhiteSpace(x.Cep));

        RuleFor(x => x.Logradouro)
            .MinimumLength(3).WithMessage("Logradouro deve ter no mínimo 3 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Logradouro));

        RuleFor(x => x.Uf)
            .Length(2).WithMessage("UF deve ter 2 caracteres")
            .When(x => !string.IsNullOrWhiteSpace(x.Uf));
    }
}
