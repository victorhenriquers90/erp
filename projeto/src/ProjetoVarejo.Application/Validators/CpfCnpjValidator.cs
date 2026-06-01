namespace ProjetoVarejo.Application.Validators;

/// <summary>
/// Validador estático para CPF e CNPJ
/// </summary>
public static class CpfCnpjValidator
{
    /// <summary>
    /// Valida se um CPF é válido usando algoritmo de dígito verificador
    /// </summary>
    public static bool ValidarCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        cpf = cpf.Replace(".", "").Replace("-", "").Trim();

        if (cpf.Length != 11 || !cpf.All(char.IsDigit))
            return false;

        // CPFs conhecidos como inválidos
        if (cpf == new string(cpf[0], 11))
            return false;

        // Calcula primeiro dígito verificador
        int soma = 0;
        for (int i = 0; i < 9; i++)
            soma += (cpf[i] - '0') * (10 - i);

        int resto = soma % 11;
        int digito1 = resto < 2 ? 0 : 11 - resto;

        if ((cpf[9] - '0') != digito1)
            return false;

        // Calcula segundo dígito verificador
        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += (cpf[i] - '0') * (11 - i);

        resto = soma % 11;
        int digito2 = resto < 2 ? 0 : 11 - resto;

        return (cpf[10] - '0') == digito2;
    }

    /// <summary>
    /// Valida se um CNPJ é válido usando algoritmo de dígito verificador
    /// </summary>
    public static bool ValidarCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return false;

        cnpj = cnpj.Replace(".", "").Replace("-", "").Replace("/", "").Trim();

        if (cnpj.Length != 14 || !cnpj.All(char.IsDigit))
            return false;

        // CNPJs conhecidos como inválidos
        if (cnpj == new string(cnpj[0], 14))
            return false;

        // Calcula primeiro dígito verificador
        int[] multiplicadores = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int soma = 0;

        for (int i = 0; i < 12; i++)
            soma += int.Parse(cnpj[i].ToString()) * multiplicadores[i];

        int digito1 = (11 - (soma % 11)) % 10;

        if (int.Parse(cnpj[12].ToString()) != digito1)
            return false;

        // Calcula segundo dígito verificador
        multiplicadores = new[] { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        soma = 0;

        for (int i = 0; i < 13; i++)
            soma += int.Parse(cnpj[i].ToString()) * multiplicadores[i];

        int digito2 = (11 - (soma % 11)) % 10;

        return int.Parse(cnpj[13].ToString()) == digito2;
    }

    /// <summary>
    /// Valida tanto CPF quanto CNPJ
    /// </summary>
    public static bool ValidarCpfOuCnpj(string documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return false;

        // Remove formatação
        string limpo = documento.Replace(".", "").Replace("-", "").Replace("/", "").Trim();

        return limpo.Length switch
        {
            11 => ValidarCpf(limpo),
            14 => ValidarCnpj(limpo),
            _ => false
        };
    }
}
