using System.Globalization;
using System.Text;

namespace ProjetoVarejo.Infrastructure.Pix;

/// <summary>
/// Gera payload PIX BR Code (Copia e Cola) conforme manual EMVCo do BACEN.
/// Estático: chave + nome + cidade + valor opcional + txid opcional + CRC16.
/// </summary>
public static class PixBrCodeBuilder
{
    public static string Gerar(string chavePix, string nomeRecebedor, string cidade, decimal? valor = null, string? txid = null)
    {
        if (string.IsNullOrWhiteSpace(chavePix)) throw new ArgumentException("Chave PIX vazia.");
        if (string.IsNullOrWhiteSpace(nomeRecebedor)) throw new ArgumentException("Nome do recebedor vazio.");
        if (string.IsNullOrWhiteSpace(cidade)) throw new ArgumentException("Cidade vazia.");

        var nome = Normalizar(nomeRecebedor).ToUpperInvariant();
        if (nome.Length > 25) nome = nome[..25];
        var cidadeNorm = Normalizar(cidade).ToUpperInvariant();
        if (cidadeNorm.Length > 15) cidadeNorm = cidadeNorm[..15];

        // Merchant Account Information (ID 26 - BR.GOV.BCB.PIX)
        var gui = TLV("00", "BR.GOV.BCB.PIX");
        var chave = TLV("01", chavePix);
        var mai = TLV("26", gui + chave);

        // Additional Data Field (ID 62) — txid
        string adf = "";
        if (!string.IsNullOrWhiteSpace(txid))
            adf = TLV("62", TLV("05", LimitarTxId(txid)));
        else
            adf = TLV("62", TLV("05", "***"));

        var sb = new StringBuilder();
        sb.Append(TLV("00", "01"));            // Payload Format Indicator
        sb.Append(TLV("01", "11"));            // Point of Initiation — 11 = estático
        sb.Append(mai);
        sb.Append(TLV("52", "0000"));          // Merchant Category Code
        sb.Append(TLV("53", "986"));           // Currency BRL
        if (valor.HasValue && valor.Value > 0)
            sb.Append(TLV("54", valor.Value.ToString("F2", CultureInfo.InvariantCulture)));
        sb.Append(TLV("58", "BR"));            // Country Code
        sb.Append(TLV("59", nome));            // Merchant Name
        sb.Append(TLV("60", cidadeNorm));      // Merchant City
        sb.Append(adf);

        // CRC final (ID 63, comprimento 04, valor a calcular)
        sb.Append("6304");
        var semCrc = sb.ToString();
        var crc = CalcularCrc16(semCrc);
        return semCrc + crc;
    }

    private static string TLV(string id, string value)
    {
        var len = value.Length.ToString().PadLeft(2, '0');
        return id + len + value;
    }

    private static string LimitarTxId(string txId)
    {
        var s = new string(txId.Where(c => char.IsLetterOrDigit(c)).ToArray());
        if (s.Length > 25) s = s[..25];
        if (s.Length < 1) s = "TX";
        return s;
    }

    private static string Normalizar(string s)
    {
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string CalcularCrc16(string payload)
    {
        ushort crc = 0xFFFF;
        var bytes = Encoding.UTF8.GetBytes(payload);
        foreach (var b in bytes)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc <<= 1;
            }
        }
        return crc.ToString("X4");
    }
}
