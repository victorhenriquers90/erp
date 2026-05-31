using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class ClienteValidator : AbstractValidator<Cliente>
{
    public ClienteValidator()
    {
        RuleFor(c => c.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório")
            .Length(3, 150).WithMessage("Nome deve ter entre 3 e 150 caracteres");

        RuleFor(c => c.CpfCnpj)
            .Must(ValidarCpfCnpj).WithMessage("CPF/CNPJ inválido")
            .When(c => !string.IsNullOrEmpty(c.CpfCnpj));

        RuleFor(c => c.Email)
            .EmailAddress().WithMessage("E-mail inválido")
            .When(c => !string.IsNullOrEmpty(c.Email));

        RuleFor(c => c.Telefone)
            .Matches(@"^\(?[1-9]{2}\)?[\s-]?9?[\s-]?\d{4}[\s-]?\d{4}$")
            .WithMessage("Telefone inválido (formato: (XX) 9XXXX-XXXX)")
            .When(c => !string.IsNullOrEmpty(c.Telefone));

        RuleFor(c => c.Cep)
            .Matches(@"^\d{5}-?\d{3}$").WithMessage("CEP inválido (formato: XXXXX-XXX)")
            .When(c => !string.IsNullOrEmpty(c.Cep));

        RuleFor(c => c.Logradouro)
            .Length(3, 150).WithMessage("Logradouro deve ter entre 3 e 150 caracteres")
            .When(c => !string.IsNullOrEmpty(c.Logradouro));

        RuleFor(c => c.Uf)
            .Length(2).WithMessage("UF deve ter exatamente 2 caracteres")
            .Matches(@"^[A-Z]{2}$").WithMessage("UF deve conter apenas letras maiúsculas")
            .When(c => !string.IsNullOrEmpty(c.Uf));

        RuleFor(c => c.LimiteCredito)
            .GreaterThanOrEqualTo(0).WithMessage("Limite de crédito deve ser maior ou igual a zero");
    }

    private static bool ValidarCpfCnpj(string? cpfCnpj)
    {
        if (string.IsNullOrWhiteSpace(cpfCnpj))
            return true;

        cpfCnpj = cpfCnpj.Replace(".", "").Replace("-", "").Replace("/", "");

        // CPF: 11 dígitos
        if (cpfCnpj.Length == 11)
            return ValidarCpf(cpfCnpj);

        // CNPJ: 14 dígitos
        if (cpfCnpj.Length == 14)
            return ValidarCnpj(cpfCnpj);

        return false;
    }

    private static bool ValidarCpf(string cpf)
    {
        if (!cpf.All(char.IsDigit)) return false;
        if (cpf.Distinct().Count() == 1) return false; // "11111111111"

        var digitos = cpf.Select(c => int.Parse(c.ToString())).ToArray();

        // Primeiro dígito verificador
        var soma1 = Enumerable.Range(0, 9)
            .Sum(i => digitos[i] * (10 - i));
        var resto1 = soma1 % 11;
        var dv1 = resto1 < 2 ? 0 : 11 - resto1;

        if (digitos[9] != dv1) return false;

        // Segundo dígito verificador
        var soma2 = Enumerable.Range(0, 10)
            .Sum(i => digitos[i] * (11 - i));
        var resto2 = soma2 % 11;
        var dv2 = resto2 < 2 ? 0 : 11 - resto2;

        return digitos[10] == dv2;
    }

    private static bool ValidarCnpj(string cnpj)
    {
        if (!cnpj.All(char.IsDigit)) return false;
        if (cnpj.Distinct().Count() == 1) return false; // "11111111111111"

        var digitos = cnpj.Select(c => int.Parse(c.ToString())).ToArray();
        var tamanho = cnpj.Length - 2;
        var numeros = digitos.Take(tamanho).ToArray();

        // Primeiro dígito verificador
        var ordem = tamanho - 7;
        var soma1 = 0;
        for (var i = 0; i < tamanho; i++)
        {
            soma1 += numeros[i] * ordem;
            ordem--;
            if (ordem < 2) ordem = 9;
        }

        var resto1 = soma1 % 11;
        var dv1 = resto1 < 2 ? 0 : 11 - resto1;

        if (digitos[tamanho] != dv1) return false;

        // Segundo dígito verificador
        tamanho++;
        numeros = digitos.Take(tamanho).ToArray();
        ordem = tamanho - 7;
        var soma2 = 0;
        for (var i = 0; i < tamanho; i++)
        {
            soma2 += numeros[i] * ordem;
            ordem--;
            if (ordem < 2) ordem = 9;
        }

        var resto2 = soma2 % 11;
        var dv2 = resto2 < 2 ? 0 : 11 - resto2;

        return digitos[tamanho] == dv2;
    }
}
