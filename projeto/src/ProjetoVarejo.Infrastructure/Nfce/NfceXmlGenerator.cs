using System.Globalization;
using System.Text;
using System.Xml.Linq;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Infrastructure.Nfce;

public class NfceXmlGenerator
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public string GerarXml(Venda venda, EmpresaConfig empresa, int numeroNfce, int serie, out string chaveAcesso, out int cNF, bool contingencia = false)
    {
        // Random.Shared evita colisão de cNF quando GerarXml é chamado em sequência rápida
        // (new Random() é semeado pelo relógio e repetiria o valor).
        cNF = Random.Shared.Next(10_000_000, 99_999_999);
        var dataEmissao = DateTime.Now;
        var tpAmb = empresa.AmbienteHomologacao ? 2 : 1;
        var tpEmis = contingencia ? 9 : 1;

        chaveAcesso = ChaveAcessoNfce.Gerar(
            ufCodigo: "35",
            dataEmissao: dataEmissao,
            cnpjEmitente: empresa.Cnpj,
            modelo: "65",
            serie: serie,
            numero: numeroNfce,
            tpEmis: tpEmis,
            cNF: cNF);

        var dv = chaveAcesso[^1].ToString();
        var idTag = "NFe" + chaveAcesso;

        var ide = new XElement(Ns + "ide",
            new XElement(Ns + "cUF", "35"),
            new XElement(Ns + "cNF", cNF.ToString("D8")),
            new XElement(Ns + "natOp", "VENDA AO CONSUMIDOR"),
            new XElement(Ns + "mod", "65"),
            new XElement(Ns + "serie", serie),
            new XElement(Ns + "nNF", numeroNfce),
            new XElement(Ns + "dhEmi", dataEmissao.ToString("yyyy-MM-ddTHH:mm:sszzz")),
            new XElement(Ns + "tpNF", "1"),
            new XElement(Ns + "idDest", "1"),
            new XElement(Ns + "cMunFG", empresa.CodigoMunicipioIbge),
            new XElement(Ns + "tpImp", "4"),
            new XElement(Ns + "tpEmis", tpEmis),
            new XElement(Ns + "cDV", dv),
            new XElement(Ns + "tpAmb", tpAmb),
            new XElement(Ns + "finNFe", "1"),
            new XElement(Ns + "indFinal", "1"),
            new XElement(Ns + "indPres", "1"),
            new XElement(Ns + "procEmi", "0"),
            new XElement(Ns + "verProc", "1.0.0"),
            contingencia ? new XElement(Ns + "dhCont", dataEmissao.ToString("yyyy-MM-ddTHH:mm:sszzz")) : null,
            contingencia ? new XElement(Ns + "xJust", "Indisponibilidade do servico de autorizacao da SEFAZ") : null);

        var emit = new XElement(Ns + "emit",
            new XElement(Ns + "CNPJ", ChaveAcessoNfce.SoNumeros(empresa.Cnpj)),
            new XElement(Ns + "xNome", empresa.RazaoSocial),
            empresa.NomeFantasia != null ? new XElement(Ns + "xFant", empresa.NomeFantasia) : null,
            new XElement(Ns + "enderEmit",
                new XElement(Ns + "xLgr", empresa.Logradouro),
                new XElement(Ns + "nro", empresa.Numero),
                empresa.Complemento != null ? new XElement(Ns + "xCpl", empresa.Complemento) : null,
                new XElement(Ns + "xBairro", empresa.Bairro),
                new XElement(Ns + "cMun", empresa.CodigoMunicipioIbge),
                new XElement(Ns + "xMun", empresa.Cidade),
                new XElement(Ns + "UF", empresa.Uf),
                new XElement(Ns + "CEP", ChaveAcessoNfce.SoNumeros(empresa.Cep)),
                new XElement(Ns + "cPais", "1058"),
                new XElement(Ns + "xPais", "BRASIL")),
            new XElement(Ns + "IE", ChaveAcessoNfce.SoNumeros(empresa.InscricaoEstadual)),
            new XElement(Ns + "CRT", empresa.RegimeTributario));

        XElement? dest = null;
        if (venda.Cliente != null && !string.IsNullOrWhiteSpace(venda.Cliente.CpfCnpj))
        {
            var doc = ChaveAcessoNfce.SoNumeros(venda.Cliente.CpfCnpj);
            dest = new XElement(Ns + "dest",
                doc.Length == 14
                    ? new XElement(Ns + "CNPJ", doc)
                    : new XElement(Ns + "CPF", doc),
                new XElement(Ns + "xNome", venda.Cliente.Nome),
                new XElement(Ns + "indIEDest", "9"));
        }

        var dets = new List<XElement>();
        int n = 1;
        foreach (var item in venda.Itens)
        {
            // Regra SEFAZ: em homologação (tpAmb=2), a descrição do 1º item DEVE ser
            // exatamente esta frase, senão a nota é rejeitada no ambiente de teste.
            var xProd = (tpAmb == 2 && n == 1)
                ? "NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL"
                : item.Produto.Descricao;

            var prod = new XElement(Ns + "prod",
                new XElement(Ns + "cProd", item.Produto.Codigo),
                new XElement(Ns + "cEAN", string.IsNullOrWhiteSpace(item.Produto.CodigoBarras) ? "SEM GTIN" : item.Produto.CodigoBarras),
                new XElement(Ns + "xProd", xProd),
                new XElement(Ns + "NCM", string.IsNullOrWhiteSpace(item.Produto.Ncm) ? "00000000" : item.Produto.Ncm),
                new XElement(Ns + "CFOP", item.Produto.Cfop),
                new XElement(Ns + "uCom", item.Produto.Unidade.ToString()),
                new XElement(Ns + "qCom", F(item.Quantidade, 4)),
                new XElement(Ns + "vUnCom", F(item.PrecoUnitario, 4)),
                new XElement(Ns + "vProd", F(item.Total, 2)),
                new XElement(Ns + "cEANTrib", string.IsNullOrWhiteSpace(item.Produto.CodigoBarras) ? "SEM GTIN" : item.Produto.CodigoBarras),
                new XElement(Ns + "uTrib", item.Produto.Unidade.ToString()),
                new XElement(Ns + "qTrib", F(item.Quantidade, 4)),
                new XElement(Ns + "vUnTrib", F(item.PrecoUnitario, 4)),
                new XElement(Ns + "indTot", "1"));

            var imposto = new XElement(Ns + "imposto",
                new XElement(Ns + "ICMS",
                    new XElement(Ns + "ICMSSN102",
                        new XElement(Ns + "orig", item.Produto.Origem),
                        new XElement(Ns + "CSOSN", "102"))),
                new XElement(Ns + "PIS",
                    new XElement(Ns + "PISNT",
                        new XElement(Ns + "CST", "49"))),
                new XElement(Ns + "COFINS",
                    new XElement(Ns + "COFINSNT",
                        new XElement(Ns + "CST", "49"))));

            dets.Add(new XElement(Ns + "det",
                new XAttribute("nItem", n++),
                prod,
                imposto));
        }

        var total = new XElement(Ns + "total",
            new XElement(Ns + "ICMSTot",
                new XElement(Ns + "vBC", "0.00"),
                new XElement(Ns + "vICMS", "0.00"),
                new XElement(Ns + "vICMSDeson", "0.00"),
                new XElement(Ns + "vFCP", "0.00"),
                new XElement(Ns + "vBCST", "0.00"),
                new XElement(Ns + "vST", "0.00"),
                new XElement(Ns + "vFCPST", "0.00"),
                new XElement(Ns + "vFCPSTRet", "0.00"),
                new XElement(Ns + "vProd", F(venda.SubTotal, 2)),
                new XElement(Ns + "vFrete", "0.00"),
                new XElement(Ns + "vSeg", "0.00"),
                new XElement(Ns + "vDesc", F(venda.Desconto, 2)),
                new XElement(Ns + "vII", "0.00"),
                new XElement(Ns + "vIPI", "0.00"),
                new XElement(Ns + "vIPIDevol", "0.00"),
                new XElement(Ns + "vPIS", "0.00"),
                new XElement(Ns + "vCOFINS", "0.00"),
                new XElement(Ns + "vOutro", "0.00"),
                new XElement(Ns + "vNF", F(venda.Total, 2)),
                new XElement(Ns + "vTotTrib", "0.00")));

        var transp = new XElement(Ns + "transp",
            new XElement(Ns + "modFrete", "9"));

        var pagItens = venda.Pagamentos.Select(p =>
            new XElement(Ns + "detPag",
                new XElement(Ns + "tPag", ((int)p.FormaPagamento).ToString("D2")),
                new XElement(Ns + "vPag", F(p.Valor, 2))));
        var pag = new XElement(Ns + "pag", pagItens);
        if (venda.Troco > 0)
            pag.Add(new XElement(Ns + "vTroco", F(venda.Troco, 2)));

        var infNFe = new XElement(Ns + "infNFe",
            new XAttribute("Id", idTag),
            new XAttribute("versao", "4.00"),
            ide, emit, dest, dets, total, transp, pag);

        var nfe = new XElement(Ns + "NFe", infNFe);

        var docXml = new XDocument(new XDeclaration("1.0", "UTF-8", null), nfe);
        var sb = new StringBuilder();
        using (var sw = new StringWriterUtf8(sb))
            docXml.Save(sw, SaveOptions.DisableFormatting);
        return sb.ToString();
    }

    /// <summary>
    /// Insere o elemento &lt;infNFeSupl&gt; com o QR Code no XML já assinado.
    /// OBRIGATÓRIO para NFC-e (modelo 65) — sem ele a SEFAZ rejeita a nota.
    /// Deve ser chamado DEPOIS de assinar: o infNFeSupl entra entre &lt;infNFe&gt; e &lt;Signature&gt;,
    /// fora da área assinada (a assinatura cobre apenas #infNFe, então não é invalidada).
    /// </summary>
    public string InserirQrCode(string xmlAssinado, EmpresaConfig empresa, bool contingencia)
    {
        XNamespace dsig = "http://www.w3.org/2000/09/xmldsig#";
        var doc = XDocument.Parse(xmlAssinado, LoadOptions.PreserveWhitespace);

        var nfe = doc.Root;
        if (nfe == null || nfe.Name != Ns + "NFe")
            throw new InvalidOperationException("XML assinado inválido: raiz <NFe> não encontrada.");

        var infNFe = nfe.Element(Ns + "infNFe")
            ?? throw new InvalidOperationException("Elemento <infNFe> não encontrado.");

        var idAttr = (string?)infNFe.Attribute("Id")
            ?? throw new InvalidOperationException("Atributo Id de <infNFe> ausente.");
        var chave = idAttr.StartsWith("NFe", StringComparison.OrdinalIgnoreCase) ? idAttr[3..] : idAttr;

        // Extrai dados do próprio XML para garantir que o QR bata com o documento.
        var dhEmiStr = (string?)infNFe.Element(Ns + "ide")?.Element(Ns + "dhEmi");
        var dataEmissao = DateTime.TryParse(dhEmiStr, out var dt) ? dt : DateTime.Now;

        var vNFStr = (string?)infNFe.Element(Ns + "total")?.Element(Ns + "ICMSTot")?.Element(Ns + "vNF");
        decimal.TryParse(vNFStr, NumberStyles.Any, InvariantCulture, out var vNF);

        var dest = infNFe.Element(Ns + "dest");
        var cpfDest = (string?)dest?.Element(Ns + "CPF") ?? (string?)dest?.Element(Ns + "CNPJ");

        var digestValue = (string?)nfe.Element(dsig + "Signature")
            ?.Element(dsig + "SignedInfo")
            ?.Element(dsig + "Reference")
            ?.Element(dsig + "DigestValue");

        // QR online (tpEmis=1) leva só chave+CSC; contingência (offline) leva dhEmi/vNF/digVal.
        var url = contingencia
            ? QrCodeNfce.GerarUrl(chave, empresa.AmbienteHomologacao, empresa.CscId, empresa.CscToken,
                                  dataEmissao, vNF, digestValue, cpfDest)
            : QrCodeNfce.GerarUrl(chave, empresa.AmbienteHomologacao, empresa.CscId, empresa.CscToken);

        var infNFeSupl = new XElement(Ns + "infNFeSupl",
            new XElement(Ns + "qrCode", new XCData(url)),
            new XElement(Ns + "urlChave", QrCodeNfce.UrlConsulta(empresa.AmbienteHomologacao)));

        // Schema exige a ordem: infNFe, infNFeSupl, Signature.
        var signature = nfe.Element(dsig + "Signature");
        if (signature != null)
            signature.AddBeforeSelf(infNFeSupl);
        else
            infNFe.AddAfterSelf(infNFeSupl);

        var sb = new StringBuilder();
        using (var sw = new StringWriterUtf8(sb))
            doc.Save(sw, SaveOptions.DisableFormatting);
        return sb.ToString();
    }

    private static string F(decimal v, int casas) => v.ToString("F" + casas, InvariantCulture);

    private class StringWriterUtf8 : StringWriter
    {
        public StringWriterUtf8(StringBuilder sb) : base(sb) { }
        public override Encoding Encoding => Encoding.UTF8;
    }
}
