using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ProjetoVarejo.Infrastructure.Nfce;

public static class QrCodeNfce
{
    public static string GerarUrl(
        string chaveAcesso,
        bool homologacao,
        string cscId,
        string cscToken,
        DateTime? dataEmissao = null,
        decimal? valorTotal = null,
        string? digestValue = null,
        string? cpfDest = null)
    {
        var nVersao = "2";
        var tpAmb = homologacao ? "2" : "1";

        var sb = new StringBuilder();
        sb.Append("chNFe=").Append(chaveAcesso);
        sb.Append("&nVersao=").Append(nVersao);
        sb.Append("&tpAmb=").Append(tpAmb);
        if (!string.IsNullOrWhiteSpace(cpfDest))
            sb.Append("&cDest=").Append(cpfDest);
        if (dataEmissao.HasValue)
            sb.Append("&dhEmi=").Append(Uri.EscapeDataString(ToHex(dataEmissao.Value.ToString("yyyy-MM-ddTHH:mm:sszzz"))));
        if (valorTotal.HasValue)
            sb.Append("&vNF=").Append(valorTotal.Value.ToString("F2", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(digestValue))
            sb.Append("&vICMS=0.00&digVal=").Append(Uri.EscapeDataString(ToHex(digestValue)));
        sb.Append("&cIdToken=").Append(cscId.PadLeft(6, '0'));

        var paramsConcat = sb.ToString() + cscToken;
        using var sha = SHA1.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(paramsConcat));
        var hashHex = Convert.ToHexString(hash).ToLowerInvariant();

        var urlBase = homologacao
            ? "https://www.homologacao.nfce.fazenda.sp.gov.br/qrcode"
            : "https://www.nfce.fazenda.sp.gov.br/qrcode";

        return urlBase + "?" + sb.ToString() + "&cHashQRCode=" + hashHex;
    }

    public static string UrlConsulta(bool homologacao) =>
        homologacao
            ? "https://www.homologacao.nfce.fazenda.sp.gov.br/consulta"
            : "https://www.nfce.fazenda.sp.gov.br/consulta";

    public static byte[] GerarImagemPng(string conteudo, int pixelSize = 6)
    {
        using var qrGen = new QRCoder.QRCodeGenerator();
        using var qrData = qrGen.CreateQrCode(conteudo, QRCoder.QRCodeGenerator.ECCLevel.M);
        var qrPng = new QRCoder.PngByteQRCode(qrData);
        return qrPng.GetGraphic(pixelSize);
    }

    private static string ToHex(string s) =>
        Convert.ToHexString(Encoding.UTF8.GetBytes(s)).ToLowerInvariant();
}
