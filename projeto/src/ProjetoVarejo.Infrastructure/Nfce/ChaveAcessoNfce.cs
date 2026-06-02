namespace ProjetoVarejo.Infrastructure.Nfce;

public static class ChaveAcessoNfce
{
    public static string Gerar(
        string ufCodigo,
        DateTime dataEmissao,
        string cnpjEmitente,
        string modelo,
        int serie,
        int numero,
        int tpEmis,
        int cNF)
    {
        var aamm = dataEmissao.ToString("yyMM");
        var cnpj = SoNumeros(cnpjEmitente).PadLeft(14, '0');
        var modeloFmt = modelo.PadLeft(2, '0');
        var serieFmt = serie.ToString().PadLeft(3, '0');
        var numeroFmt = numero.ToString().PadLeft(9, '0');
        var tpEmisFmt = tpEmis.ToString();
        var cNFFmt = cNF.ToString().PadLeft(8, '0');

        var chave43 = ufCodigo + aamm + cnpj + modeloFmt + serieFmt + numeroFmt + tpEmisFmt + cNFFmt;
        var dv = CalcularDv(chave43);
        return chave43 + dv;
    }

    public static int CalcularDv(string chave43)
    {
        if (chave43.Length != 43) throw new ArgumentException("Chave deve ter 43 dígitos.");
        var pesos = new[] { 2, 3, 4, 5, 6, 7, 8, 9 };
        int soma = 0;
        for (int i = 0; i < 43; i++)
        {
            var digito = int.Parse(chave43[42 - i].ToString());
            var peso = pesos[i % 8];
            soma += digito * peso;
        }
        var resto = soma % 11;
        var dv = 11 - resto;
        return dv >= 10 ? 0 : dv;
    }

    public static string SoNumeros(string s) =>
        new(s?.Where(char.IsDigit).ToArray() ?? Array.Empty<char>());
}
