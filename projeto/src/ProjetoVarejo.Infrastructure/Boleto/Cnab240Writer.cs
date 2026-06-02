using System.Globalization;
using System.Text;

namespace ProjetoVarejo.Infrastructure.Boleto;

/// <summary>
/// Gerador simplificado de remessa CNAB 240 para registro de boletos.
/// Layout genérico — bancos exigem ajustes específicos (Itaú/BB/Bradesco têm variações).
/// </summary>
public class Cnab240Writer
{
    public string GerarRemessa(List<BoletoBancario> boletos, int numeroArquivo)
    {
        if (boletos.Count == 0) throw new ArgumentException("Lista vazia.");
        var benef = boletos[0].Beneficiario;
        var sb = new StringBuilder();
        var lote = 1;
        int seqLote = 0;

        // Header de Arquivo
        sb.Append(Linha($"{Banco(benef)}0000         " +
            $"2{Pad(benef.CnpjCpf, 14, '0', alinhar: Alinhar.Direita)}" +
            new string(' ', 20) +
            Pad(benef.Agencia, 5, '0') +
            " " +
            Pad(benef.Conta, 12, '0') +
            " " +
            " " +
            Pad(benef.Nome, 30) +
            Pad("ProjetoVarejo", 30) +
            "1" +
            DateTime.Now.ToString("ddMMyyyy") +
            DateTime.Now.ToString("HHmmss") +
            Pad(numeroArquivo.ToString(), 6, '0') +
            "104" +  // versão layout (104 = CNAB240 v8.4)
            "00000" +
            new string(' ', 20 + 20 + 29)));

        // Header de Lote
        sb.Append(Linha($"{Banco(benef)}{Pad(lote.ToString(), 4, '0')}1R01  030" +
            $"2{Pad(benef.CnpjCpf, 14, '0', alinhar: Alinhar.Direita)}" +
            new string(' ', 20) +
            Pad(benef.Agencia, 5, '0') + " " + Pad(benef.Conta, 12, '0') + " " + " " +
            Pad(benef.Nome, 30) +
            new string(' ', 40 + 40) +
            Pad(numeroArquivo.ToString(), 8, '0') +
            DateTime.Now.ToString("ddMMyyyy") +
            new string(' ', 8 + 33)));

        // Detalhes
        foreach (var b in boletos)
        {
            seqLote++;
            sb.Append(Linha($"{Banco(benef)}{Pad(lote.ToString(), 4, '0')}3{Pad(seqLote.ToString(), 5, '0')}P 01" +
                Pad(benef.Agencia, 5, '0') + " " + Pad(benef.Conta, 12, '0') + " " + " " +
                Pad(b.NossoNumero, 20) +
                Pad(b.Beneficiario.Carteira, 1) +
                "1" + "1" + "2" + "2" +
                Pad(b.NumeroDocumento, 15) +
                b.DataVencimento.ToString("ddMMyyyy") +
                Pad(((long)(b.Valor * 100)).ToString(), 15, '0') +
                new string('0', 5) + " " +
                "01" +
                b.DataEmissao.ToString("ddMMyyyy") +
                "000" + Pad("0", 13, '0') +
                "0" + "00" + new string('0', 13) +
                "0" + "00" + new string('0', 13) +
                new string(' ', 25) +
                "0" + new string('0', 8 + 3) +
                new string(' ', 11) + "  "));
        }

        // Trailer de Lote
        sb.Append(Linha($"{Banco(benef)}{Pad(lote.ToString(), 4, '0')}5         " +
            Pad((seqLote + 2).ToString(), 6, '0') +
            Pad(seqLote.ToString(), 6, '0') + new string('0', 17) +
            Pad(seqLote.ToString(), 6, '0') + new string('0', 17) +
            Pad("0", 6, '0') + new string('0', 17) +
            new string(' ', 31 + 117)));

        // Trailer de Arquivo
        sb.Append(Linha($"{Banco(benef)}9999         " +
            "000001" + Pad((seqLote + 4).ToString(), 6, '0') + "000000" +
            new string(' ', 205)));

        return sb.ToString();
    }

    private static string Linha(string conteudo)
    {
        var c = conteudo.Length > 240 ? conteudo[..240] : conteudo.PadRight(240);
        return c + "\r\n";
    }

    private static string Banco(DadosBeneficiario b) => b.CodigoBanco.ToString().PadLeft(3, '0');

    private enum Alinhar { Esquerda, Direita }
    private static string Pad(string? s, int len, char padChar = ' ', Alinhar alinhar = Alinhar.Esquerda)
    {
        s ??= "";
        if (s.Length > len) return alinhar == Alinhar.Direita ? s[^len..] : s[..len];
        return alinhar == Alinhar.Direita ? s.PadLeft(len, padChar) : s.PadRight(len, padChar);
    }
}
