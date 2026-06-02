using System.Text;
using System.Xml.Linq;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Infrastructure.Nfce;

public class NfceCancelamentoBuilder
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";

    public string GerarXmlEvento(NotaFiscal nota, EmpresaConfig empresa, string justificativa, int nSeqEvento, out string idEvento)
    {
        if (string.IsNullOrWhiteSpace(nota.ChaveAcesso) || nota.ChaveAcesso.Length != 44)
            throw new ArgumentException("Nota sem chave de acesso válida.");
        if (string.IsNullOrWhiteSpace(nota.Protocolo))
            throw new ArgumentException("Nota sem protocolo de autorização.");
        if (justificativa == null || justificativa.Length < 15)
            throw new ArgumentException("Justificativa deve ter no mínimo 15 caracteres.");
        if (justificativa.Length > 255)
            throw new ArgumentException("Justificativa deve ter no máximo 255 caracteres.");
        if (nSeqEvento < 1 || nSeqEvento > 99)
            throw new ArgumentException("nSeqEvento fora do intervalo (1-99).");

        var tpEvento = "110111";
        var verEvento = "1.00";
        idEvento = "ID" + tpEvento + nota.ChaveAcesso + nSeqEvento.ToString("D2");

        var tpAmb = empresa.AmbienteHomologacao ? "2" : "1";
        var cnpj = ChaveAcessoNfce.SoNumeros(empresa.Cnpj);

        var detEvento = new XElement(Ns + "detEvento",
            new XAttribute("versao", verEvento),
            new XElement(Ns + "descEvento", "Cancelamento"),
            new XElement(Ns + "nProt", nota.Protocolo),
            new XElement(Ns + "xJust", justificativa));

        var infEvento = new XElement(Ns + "infEvento",
            new XAttribute("Id", idEvento),
            new XElement(Ns + "cOrgao", "35"),
            new XElement(Ns + "tpAmb", tpAmb),
            new XElement(Ns + "CNPJ", cnpj),
            new XElement(Ns + "chNFe", nota.ChaveAcesso),
            new XElement(Ns + "dhEvento", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:sszzz")),
            new XElement(Ns + "tpEvento", tpEvento),
            new XElement(Ns + "nSeqEvento", nSeqEvento.ToString()),
            new XElement(Ns + "verEvento", verEvento),
            detEvento);

        var evento = new XElement(Ns + "evento",
            new XAttribute("versao", verEvento),
            infEvento);

        var docXml = new XDocument(new XDeclaration("1.0", "UTF-8", null), evento);
        var sb = new StringBuilder();
        using (var sw = new StringWriterUtf8(sb))
            docXml.Save(sw, SaveOptions.DisableFormatting);
        return sb.ToString();
    }

    public string EnveloparLote(string eventoXmlAssinado, int idLote)
    {
        var idx = eventoXmlAssinado.IndexOf("<evento", StringComparison.Ordinal);
        if (idx < 0) throw new InvalidOperationException("XML evento não encontrado.");
        var corpo = eventoXmlAssinado.Substring(idx);

        return
$@"<envEvento xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""1.00"">
<idLote>{idLote}</idLote>
{corpo}
</envEvento>";
    }

    private class StringWriterUtf8 : StringWriter
    {
        public StringWriterUtf8(StringBuilder sb) : base(sb) { }
        public override Encoding Encoding => Encoding.UTF8;
    }
}
