using FluentValidation;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Validators;

public class FornecedorValidator : AbstractValidator<Fornecedor>
{
    public FornecedorValidator()
    {
        RuleFor(f => f.RazaoSocial)
            .NotEmpty().WithMessage("Razão social é obrigatória")
            .Length(3, 150).WithMessage("Razão social deve ter entre 3 e 150 caracteres");

        RuleFor(f => f.Cnpj)
            .NotEmpty().WithMessage("CNPJ é obrigatório")
            .Must(ValidarCnpj).WithMessage("CNPJ inválido");

        RuleFor(f => f.NomeFantasia)
            .Length(3, 150).WithMessage("Nome fantasia deve ter entre 3 e 150 caracteres")
            .When(f => !string.IsNullOrEmpty(f.NomeFantasia));

        RuleFor(f => f.Email)
            .EmailAddress().WithMessage("E-mail inválido")
            .When(f => !string.IsNullOrEmpty(f.Email));

        RuleFor(f => f.Telefone)
            .Matches(@"^\(?[1-9]{2}\)?[\s-]?9?[\s-]?\d{4}[\s-]?\d{4}$")
            .WithMessage("Telefone inválido (formato: (XX) 9XXXX-XXXX)")
            .When(f => !string.IsNullOrEmpty(f.Telefone));

        RuleFor(f => f.Contato)
            .Length(3, 100).WithMessage("Contato deve ter entre 3 e 100 caracteres")
            .When(f => !string.IsNullOrEmpty(f.Contato));

        RuleFor(f => f.Cep)
            .Matches(@"^\d{5}-?\d{3}$").WithMessage("CEP inválido (formato: XXXXX-XXX)")
            .When(f => !string.IsNullOrEmpty(f.Cep));

        RuleFor(f => f.Logradouro)
            .Length(3, 150).WithMessage("Logradouro deve ter entre 3 e 150 caracteres")
            .When(f => !string.IsNullOrEmpty(f.Logradouro));

        RuleFor(f => f.Uf)
            .Length(2).WithMessage("UF deve ter exatamente 2 caracteres")
            .Matches(@"^[A-Z]{2}$").WithMessage("UF deve conter apenas letras maiúsculas")
            .When(f => !string.IsNullOrEmpty(f.Uf));

        RuleFor(f => f.InscricaoEstadual)
            .Length(0, 20).WithMessage("Inscrição estadual deve ter no máximo 20 caracteres")
            .When(f => !string.IsNullOrEmpty(f.InscricaoEstadual));
    }

    private static bool ValidarCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return false;

        cnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "");

        if (cnpj.Length != 14) return false;
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
