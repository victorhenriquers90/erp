using System.Globalization;
using System.Text;

namespace ProjetoVarejo.Infrastructure.Boleto;

public class DadosBeneficiario
{
    public string CnpjCpf { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Agencia { get; set; } = "";
    public string Conta { get; set; } = "";
    public string Carteira { get; set; } = "";
    public int CodigoBanco { get; set; }       // 001=BB, 237=Bradesco, 341=Itaú, 077=Inter, 033=Santander, 104=CEF
    public string CodigoBeneficiario { get; set; } = "";
}

public class DadosPagador
{
    public string CnpjCpf { get; set; } = "";
    public string Nome { get; set; } = "";
    public string Endereco { get; set; } = "";
    public string Bairro { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Uf { get; set; } = "";
    public string Cep { get; set; } = "";
}

public class BoletoBancario
{
    public DadosBeneficiario Beneficiario { get; set; } = new();
    public DadosPagador Pagador { get; set; } = new();
    public string NossoNumero { get; set; } = "";
    public decimal Valor { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime DataEmissao { get; set; } = DateTime.Today;
    public string NumeroDocumento { get; set; } = "";
    public string Instrucoes { get; set; } = "";
    public string Demonstrativo { get; set; } = "";

    public string CodigoBarras { get; private set; } = "";
    public string LinhaDigitavel { get; private set; } = "";

    public void Calcular()
    {
        CodigoBarras = MontarCodigoBarras();
        LinhaDigitavel = FormatarLinhaDigitavel(CodigoBarras);
    }

    private string MontarCodigoBarras()
    {
        // Layout FEBRABAN 44 dígitos:
        // pos 1-3 = código do banco
        // pos 4   = código da moeda (9 = real)
        // pos 5   = DV
        // pos 6-9 = fator de vencimento
        // pos 10-19 = valor (10 dígitos, sem vírgula, zero à esquerda)
        // pos 20-44 = campo livre (depende do banco)
        var banco = Beneficiario.CodigoBanco.ToString().PadLeft(3, '0');
        var moeda = "9";
        var fator = FatorVencimento(DataVencimento).ToString().PadLeft(4, '0');
        var valor = ((long)(Valor * 100)).ToString().PadLeft(10, '0');

        var campoLivre = MontarCampoLivre();
        if (campoLivre.Length != 25) throw new InvalidOperationException("Campo livre deve ter 25 dígitos.");

        var semDv = banco + moeda + fator + valor + campoLivre;
        var dv = CalcularDvCodigoBarras(semDv);
        return banco + moeda + dv + fator + valor + campoLivre;
    }

    /// <summary>
    /// Campo livre genérico — formato simplificado.
    /// Para um banco específico, customizar conforme manual.
    /// </summary>
    private string MontarCampoLivre()
    {
        // Padrão usado por vários bancos (Bradesco, Santander, etc.):
        // agência(4) + carteira(2) + nosso número(11) + conta(7) + zero(1)
        var ag = SoNumeros(Beneficiario.Agencia).PadLeft(4, '0');
        if (ag.Length > 4) ag = ag[^4..];
        var carteira = SoNumeros(Beneficiario.Carteira).PadLeft(2, '0');
        if (carteira.Length > 2) carteira = carteira[^2..];
        var nn = SoNumeros(NossoNumero).PadLeft(11, '0');
        if (nn.Length > 11) nn = nn[^11..];
        var conta = SoNumeros(Beneficiario.Conta).PadLeft(7, '0');
        if (conta.Length > 7) conta = conta[^7..];
        return ag + carteira + nn + conta + "0";
    }

    private static int FatorVencimento(DateTime venc)
    {
        // Base FEBRABAN: 07/10/1997 = 1000. Modulo 9000 a partir de 22/02/2025.
        var baseDate = new DateTime(1997, 10, 7);
        var dias = (venc.Date - baseDate).Days;
        return dias % 9000 == 0 ? 9000 : dias % 9000 < 1000 ? 1000 + (dias % 9000) : dias % 9000;
    }

    private static int CalcularDvCodigoBarras(string semDv)
    {
        // Módulo 11 com pesos 2-9 da direita para esquerda
        int soma = 0, peso = 2;
        for (int i = semDv.Length - 1; i >= 0; i--)
        {
            soma += int.Parse(semDv[i].ToString()) * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }
        var resto = soma % 11;
        var dv = 11 - resto;
        if (dv == 0 || dv == 10 || dv == 11) return 1;
        return dv;
    }

    private static string FormatarLinhaDigitavel(string codigoBarras)
    {
        // Campo 1: pos 1-3 (banco) + 4 (moeda) + 20-24 (5 primeiros campo livre) + DV
        var c1 = codigoBarras.Substring(0, 4) + codigoBarras.Substring(19, 5);
        var c2 = codigoBarras.Substring(24, 10);
        var c3 = codigoBarras.Substring(34, 10);
        var c4 = codigoBarras.Substring(4, 1);                 // DV geral
        var c5 = codigoBarras.Substring(5, 14);                // fator + valor

        var dv1 = Mod10(c1).ToString();
        var dv2 = Mod10(c2).ToString();
        var dv3 = Mod10(c3).ToString();

        return $"{c1[..5]}.{c1[5..]}{dv1} {c2[..5]}.{c2[5..]}{dv2} {c3[..5]}.{c3[5..]}{dv3} {c4} {c5}";
    }

    private static int Mod10(string campo)
    {
        int soma = 0, peso = 2;
        for (int i = campo.Length - 1; i >= 0; i--)
        {
            var v = int.Parse(campo[i].ToString()) * peso;
            if (v > 9) v = (v / 10) + (v % 10);
            soma += v;
            peso = peso == 2 ? 1 : 2;
        }
        var resto = soma % 10;
        var dv = 10 - resto;
        return dv == 10 ? 0 : dv;
    }

    private static string SoNumeros(string s) => new(s.Where(char.IsDigit).ToArray());
}
