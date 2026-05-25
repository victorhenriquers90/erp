using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace ProjetoVarejo.Infrastructure.Nfce;

public class NfceAssinador
{
    public string Assinar(string xml, string caminhoPfx, string senhaPfx, string idReferencia)
    {
        using var cert = new X509Certificate2(
            caminhoPfx, senhaPfx,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);

        if (!cert.HasPrivateKey)
            throw new InvalidOperationException("Certificado não contém chave privada.");

        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);

        var signed = new SignedXml(doc)
        {
            SigningKey = cert.GetRSAPrivateKey()
        };
        signed.SignedInfo!.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signed.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

        var reference = new Reference
        {
            Uri = "#" + idReferencia,
            DigestMethod = SignedXml.XmlDsigSHA1Url
        };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signed.AddReference(reference);

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(new KeyInfoX509Data(cert));
        signed.KeyInfo = keyInfo;

        signed.ComputeSignature();
        var signatureElem = signed.GetXml();

        var nfeNode = doc.DocumentElement;
        if (nfeNode == null) throw new InvalidOperationException("XML inválido.");
        var importado = doc.ImportNode(signatureElem, true);
        nfeNode.AppendChild(importado);

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings
        {
            OmitXmlDeclaration = false,
            Encoding = new UTF8Encoding(false),
            Indent = false
        };
        using (var sw = new StringWriterUtf8(sb))
        using (var xw = XmlWriter.Create(sw, settings))
            doc.Save(xw);
        return sb.ToString();
    }

    private class StringWriterUtf8 : StringWriter
    {
        public StringWriterUtf8(StringBuilder sb) : base(sb) { }
        public override Encoding Encoding => Encoding.UTF8;
    }
}
