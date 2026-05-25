using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Infrastructure.Nfce;

public class SefazSpClient
{
    private static readonly XNamespace SoapNs = "http://www.w3.org/2003/05/soap-envelope";
    private static readonly XNamespace NfeNs = "http://www.portalfiscal.inf.br/nfe";

    public class RetornoSefaz
    {
        public string CStat { get; set; } = "";
        public string XMotivo { get; set; } = "";
        public string? NProt { get; set; }
        public string? ChNFe { get; set; }
        public string XmlRetornoCompleto { get; set; } = "";
        public bool Autorizada => CStat == "100";
        public bool Denegada => CStat == "110" || CStat == "301" || CStat == "302";
    }

    public async Task<RetornoSefaz> EnviarAutorizacaoAsync(
        string xmlNfeAssinado,
        string caminhoPfx,
        string senhaPfx,
        bool homologacao,
        int idLote)
    {
        var url = homologacao
            ? "https://homologacao.nfce.fazenda.sp.gov.br/ws/NFeAutorizacao4.asmx"
            : "https://nfce.fazenda.sp.gov.br/ws/NFeAutorizacao4.asmx";

        var nfeMatch = ExtrairTag(xmlNfeAssinado, "<NFe", "</NFe>");
        if (nfeMatch == null) throw new InvalidOperationException("Tag NFe não encontrada no XML assinado.");

        var enviNFe =
$@"<enviNFe xmlns=""http://www.portalfiscal.inf.br/nfe"" versao=""4.00"">
<idLote>{idLote}</idLote>
<indSinc>1</indSinc>
{nfeMatch}
</enviNFe>";

        var soap =
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Body>
    <nfeDadosMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4"">
{enviNFe}
    </nfeDadosMsg>
  </soap12:Body>
</soap12:Envelope>";

        using var cert = new X509Certificate2(
            caminhoPfx, senhaPfx,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
        };
        handler.ClientCertificates.Add(cert);

        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        var content = new StringContent(soap, new UTF8Encoding(false), "application/soap+xml");
        content.Headers.ContentType!.CharSet = "utf-8";
        content.Headers.ContentType.Parameters.Clear();
        content.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("charset", "utf-8"));
        content.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("action", "\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeAutorizacao4/nfeAutorizacaoLote\""));

        HttpResponseMessage resp;
        try
        {
            resp = await http.PostAsync(url, content);
        }
        catch (Exception ex)
        {
            return new RetornoSefaz
            {
                CStat = "999",
                XMotivo = $"Falha de comunicação: {ex.Message}",
                XmlRetornoCompleto = ex.ToString()
            };
        }

        var body = await resp.Content.ReadAsStringAsync();
        return ParseRetorno(body);
    }

    private RetornoSefaz ParseRetorno(string body)
    {
        var ret = new RetornoSefaz { XmlRetornoCompleto = body };
        try
        {
            var doc = XDocument.Parse(body);
            var protNFe = doc.Descendants(NfeNs + "protNFe").FirstOrDefault();
            if (protNFe != null)
            {
                var infProt = protNFe.Element(NfeNs + "infProt");
                if (infProt != null)
                {
                    ret.CStat = infProt.Element(NfeNs + "cStat")?.Value ?? "";
                    ret.XMotivo = infProt.Element(NfeNs + "xMotivo")?.Value ?? "";
                    ret.NProt = infProt.Element(NfeNs + "nProt")?.Value;
                    ret.ChNFe = infProt.Element(NfeNs + "chNFe")?.Value;
                    return ret;
                }
            }

            var retEnvi = doc.Descendants(NfeNs + "retEnviNFe").FirstOrDefault();
            if (retEnvi != null)
            {
                ret.CStat = retEnvi.Element(NfeNs + "cStat")?.Value ?? "";
                ret.XMotivo = retEnvi.Element(NfeNs + "xMotivo")?.Value ?? "";
            }
        }
        catch (Exception ex)
        {
            ret.CStat = "999";
            ret.XMotivo = "Falha ao interpretar retorno: " + ex.Message;
        }
        return ret;
    }

    private static string? ExtrairTag(string xml, string tagInicio, string tagFim)
    {
        var idx = xml.IndexOf(tagInicio, StringComparison.Ordinal);
        if (idx < 0) return null;
        var fim = xml.IndexOf(tagFim, idx, StringComparison.Ordinal);
        if (fim < 0) return null;
        return xml.Substring(idx, fim - idx + tagFim.Length);
    }

    public async Task<RetornoSefaz> EnviarEventoAsync(
        string envEventoXml,
        string caminhoPfx,
        string senhaPfx,
        bool homologacao)
    {
        var url = homologacao
            ? "https://homologacao.nfce.fazenda.sp.gov.br/ws/NFeRecepcaoEvento4.asmx"
            : "https://nfce.fazenda.sp.gov.br/ws/NFeRecepcaoEvento4.asmx";

        var soap =
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Body>
    <nfeDadosMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4"">
{envEventoXml}
    </nfeDadosMsg>
  </soap12:Body>
</soap12:Envelope>";

        return await EnviarSoapAsync(url, soap, caminhoPfx, senhaPfx,
            "\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeRecepcaoEvento4/nfeRecepcaoEvento\"",
            ParseRetornoEvento);
    }

    public async Task<RetornoSefaz> EnviarInutilizacaoAsync(
        string inutNFeAssinadoXml,
        string caminhoPfx,
        string senhaPfx,
        bool homologacao)
    {
        var url = homologacao
            ? "https://homologacao.nfce.fazenda.sp.gov.br/ws/NFeInutilizacao4.asmx"
            : "https://nfce.fazenda.sp.gov.br/ws/NFeInutilizacao4.asmx";

        var idx = inutNFeAssinadoXml.IndexOf("<inutNFe", StringComparison.Ordinal);
        var corpo = idx >= 0 ? inutNFeAssinadoXml.Substring(idx) : inutNFeAssinadoXml;

        var soap =
$@"<?xml version=""1.0"" encoding=""utf-8""?>
<soap12:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap12=""http://www.w3.org/2003/05/soap-envelope"">
  <soap12:Body>
    <nfeDadosMsg xmlns=""http://www.portalfiscal.inf.br/nfe/wsdl/NFeInutilizacao4"">
{corpo}
    </nfeDadosMsg>
  </soap12:Body>
</soap12:Envelope>";

        return await EnviarSoapAsync(url, soap, caminhoPfx, senhaPfx,
            "\"http://www.portalfiscal.inf.br/nfe/wsdl/NFeInutilizacao4/nfeInutilizacaoNF\"",
            ParseRetornoInutilizacao);
    }

    private async Task<RetornoSefaz> EnviarSoapAsync(
        string url, string soap, string caminhoPfx, string senhaPfx,
        string action, Func<string, RetornoSefaz> parser)
    {
        using var cert = new X509Certificate2(
            caminhoPfx, senhaPfx,
            X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
        };
        handler.ClientCertificates.Add(cert);

        using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        var content = new StringContent(soap, new UTF8Encoding(false), "application/soap+xml");
        content.Headers.ContentType!.CharSet = "utf-8";
        content.Headers.ContentType.Parameters.Clear();
        content.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("charset", "utf-8"));
        content.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue("action", action));

        try
        {
            var resp = await http.PostAsync(url, content);
            var body = await resp.Content.ReadAsStringAsync();
            return parser(body);
        }
        catch (Exception ex)
        {
            return new RetornoSefaz
            {
                CStat = "999",
                XMotivo = $"Falha de comunicação: {ex.Message}",
                XmlRetornoCompleto = ex.ToString()
            };
        }
    }

    private RetornoSefaz ParseRetornoEvento(string body)
    {
        var ret = new RetornoSefaz { XmlRetornoCompleto = body };
        try
        {
            var doc = XDocument.Parse(body);
            var infEvento = doc.Descendants(NfeNs + "infEvento").FirstOrDefault();
            if (infEvento != null)
            {
                ret.CStat = infEvento.Element(NfeNs + "cStat")?.Value ?? "";
                ret.XMotivo = infEvento.Element(NfeNs + "xMotivo")?.Value ?? "";
                ret.NProt = infEvento.Element(NfeNs + "nProt")?.Value;
                ret.ChNFe = infEvento.Element(NfeNs + "chNFe")?.Value;
                return ret;
            }
            var retEnv = doc.Descendants(NfeNs + "retEnvEvento").FirstOrDefault();
            if (retEnv != null)
            {
                ret.CStat = retEnv.Element(NfeNs + "cStat")?.Value ?? "";
                ret.XMotivo = retEnv.Element(NfeNs + "xMotivo")?.Value ?? "";
            }
        }
        catch (Exception ex)
        {
            ret.CStat = "999";
            ret.XMotivo = "Falha ao interpretar retorno: " + ex.Message;
        }
        return ret;
    }

    private RetornoSefaz ParseRetornoInutilizacao(string body)
    {
        var ret = new RetornoSefaz { XmlRetornoCompleto = body };
        try
        {
            var doc = XDocument.Parse(body);
            var infInut = doc.Descendants(NfeNs + "infInut").FirstOrDefault();
            if (infInut != null)
            {
                ret.CStat = infInut.Element(NfeNs + "cStat")?.Value ?? "";
                ret.XMotivo = infInut.Element(NfeNs + "xMotivo")?.Value ?? "";
                ret.NProt = infInut.Element(NfeNs + "nProt")?.Value;
            }
        }
        catch (Exception ex)
        {
            ret.CStat = "999";
            ret.XMotivo = "Falha ao interpretar retorno: " + ex.Message;
        }
        return ret;
    }
}
