using System.Text;
using System.Xml.Linq;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Infrastructure.Nfce;

public class NfceInutilizacaoBuilder
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";

    public string GerarXml(EmpresaConfig empresa, int serie, int nNFIni, int nNFFin, string justificativa, out string idInutilizacao)
    {
        if (justificativa == null || justificativa.Length < 15)
            throw new ArgumentException("Justificativa deve ter no mínimo 15 caracteres.");
        if (justificativa.Length > 255)
            throw new ArgumentException("Justificativa deve ter no máximo 255 caracteres.");
        if (nNFIni > nNFFin) throw new ArgumentException("Número inicial maior que final.");
        if (serie < 0 || serie > 999) throw new ArgumentException("Série inválida.");

        var cUF = "35";
        var ano = DateTime.Now.ToString("yy");
        var cnpj = ChaveAcessoNfce.SoNumeros(empresa.Cnpj).PadLeft(14, '0');
        var modelo = "65";
        var serieStr = serie.ToString().PadLeft(3, '0');
        var iniStr = nNFIni.ToString().PadLeft(9, '0');
        var finStr = nNFFin.ToString().PadLeft(9, '0');

        var chave42 = cUF + ano + cnpj + modelo + serieStr + iniStr + finStr;

        var pesos = new[] { 2, 3, 4, 5, 6, 7, 8, 9 };
        int soma = 0;
        for (int i = 0; i < 42; i++)
        {
            var dig = int.Parse(chave42[41 - i].ToString());
            soma += dig * pesos[i % 8];
        }
        var resto = soma % 11;
        var dv = 11 - resto;
        if (dv >= 10) dv = 0;
        var chave43 = chave42 + dv.ToString();
        idInutilizacao = "ID" + chave43;

        var tpAmb = empresa.AmbienteHomologacao ? "2" : "1";

        var infInut = new XElement(Ns + "infInut",
            new XAttribute("Id", idInutilizacao),
            new XElement(Ns + "tpAmb", tpAmb),
            new XElement(Ns + "xServ", "INUTILIZAR"),
            new XElement(Ns + "cUF", cUF),
            new XElement(Ns + "ano", ano),
            new XElement(Ns + "CNPJ", cnpj),
            new XElement(Ns + "mod", modelo),
            new XElement(Ns + "serie", serie.ToString()),
            new XElement(Ns + "nNFIni", nNFIni.ToString()),
            new XElement(Ns + "nNFFin", nNFFin.ToString()),
            new XElement(Ns + "xJust", justificativa));

        var inutNFe = new XElement(Ns + "inutNFe",
            new XAttribute("versao", "4.00"),
            infInut);

        var docXml = new XDocument(new XDeclaration("1.0", "UTF-8", null), inutNFe);
        var sb = new StringBuilder();
        using (var sw = new StringWriterUtf8(sb))
            docXml.Save(sw, SaveOptions.DisableFormatting);
        return sb.ToString();
    }

    private class StringWriterUtf8 : StringWriter
    {
        public StringWriterUtf8(StringBuilder sb) : base(sb) { }
        public override Encoding Encoding => Encoding.UTF8;
    }
}
