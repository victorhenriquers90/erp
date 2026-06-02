using System.Globalization;
using System.Text;
using System.Xml.Linq;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Nfce;

namespace ProjetoVarejo.Infrastructure.Nfe;

/// <summary>
/// Gerador de XML da NF-e (modelo 55 — operação B2B / faturamento).
/// Diferenças vs NFC-e (65): destinatário obrigatório com endereço completo,
/// sem QR Code / CSC (infNFeSupl), DANFE retrato (tpImp=1), não consumidor final.
///
/// LACUNA conhecida: a entidade Cliente ainda não armazena o código IBGE do
/// município do destinatário (exigido em enderDest/cMun). Enquanto não houver,
/// usa-se o código do emitente como fallback (válido quando mesma cidade).
/// </summary>
public class NfeXmlGenerator
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public string GerarXml(Venda venda, EmpresaConfig empresa, int numero, int serie, out string chaveAcesso, out int cNF)
    {
        if (venda.Cliente == null || string.IsNullOrWhiteSpace(venda.Cliente.CpfCnpj))
            throw new InvalidOperationException("NF-e exige destinatário (cliente com CPF/CNPJ).");

        cNF = Random.Shared.Next(10_000_000, 99_999_999);
        var dataEmissao = DateTime.Now;
        var tpAmb = empresa.AmbienteHomologacao ? 2 : 1;
        const int tpEmis = 1;

        var cliente = venda.Cliente;
        var docDest = ChaveAcessoNfce.SoNumeros(cliente.CpfCnpj!);
        var ufDest = string.IsNullOrWhiteSpace(cliente.Uf) ? empresa.Uf : cliente.Uf!;
        var idDest = string.Equals(ufDest, empresa.Uf, StringComparison.OrdinalIgnoreCase) ? "1" : "2";

        chaveAcesso = ChaveAcessoNfce.Gerar(
            ufCodigo: "35", dataEmissao: dataEmissao, cnpjEmitente: empresa.Cnpj,
            modelo: "55", serie: serie, numero: numero, tpEmis: tpEmis, cNF: cNF);

        var dv = chaveAcesso[^1].ToString();
        var idTag = "NFe" + chaveAcesso;

        var ide = new XElement(Ns + "ide",
            new XElement(Ns + "cUF", "35"),
            new XElement(Ns + "cNF", cNF.ToString("D8")),
            new XElement(Ns + "natOp", "VENDA"),
            new XElement(Ns + "mod", "55"),
            new XElement(Ns + "serie", serie),
            new XElement(Ns + "nNF", numero),
            new XElement(Ns + "dhEmi", dataEmissao.ToString("yyyy-MM-ddTHH:mm:sszzz")),
            new XElement(Ns + "dhSaiEnt", dataEmissao.ToString("yyyy-MM-ddTHH:mm:sszzz")),
            new XElement(Ns + "tpNF", "1"),       // 1 = saída
            new XElement(Ns + "idDest", idDest),
            new XElement(Ns + "cMunFG", empresa.CodigoMunicipioIbge),
            new XElement(Ns + "tpImp", "1"),       // DANFE retrato
            new XElement(Ns + "tpEmis", tpEmis),
            new XElement(Ns + "cDV", dv),
            new XElement(Ns + "tpAmb", tpAmb),
            new XElement(Ns + "finNFe", "1"),
            new XElement(Ns + "indFinal", "0"),    // não é consumidor final (B2B)
            new XElement(Ns + "indPres", "9"),     // operação não presencial / outros
            new XElement(Ns + "procEmi", "0"),
            new XElement(Ns + "verProc", "1.0.0"));

        var emit = new XElement(Ns + "emit",
            new XElement(Ns + "CNPJ", ChaveAcessoNfce.SoNumeros(empresa.Cnpj)),
            new XElement(Ns + "xNome", empresa.RazaoSocial),
            string.IsNullOrWhiteSpace(empresa.NomeFantasia) ? null : new XElement(Ns + "xFant", empresa.NomeFantasia),
            new XElement(Ns + "enderEmit",
                new XElement(Ns + "xLgr", empresa.Logradouro),
                new XElement(Ns + "nro", empresa.Numero),
                string.IsNullOrWhiteSpace(empresa.Complemento) ? null : new XElement(Ns + "xCpl", empresa.Complemento),
                new XElement(Ns + "xBairro", empresa.Bairro),
                new XElement(Ns + "cMun", empresa.CodigoMunicipioIbge),
                new XElement(Ns + "xMun", empresa.Cidade),
                new XElement(Ns + "UF", empresa.Uf),
                new XElement(Ns + "CEP", ChaveAcessoNfce.SoNumeros(empresa.Cep)),
                new XElement(Ns + "cPais", "1058"),
                new XElement(Ns + "xPais", "BRASIL")),
            new XElement(Ns + "IE", ChaveAcessoNfce.SoNumeros(empresa.InscricaoEstadual)),
            new XElement(Ns + "CRT", empresa.RegimeTributario));

        var contribuinte = !string.IsNullOrWhiteSpace(cliente.InscricaoEstadual);
        var dest = new XElement(Ns + "dest",
            docDest.Length == 14 ? new XElement(Ns + "CNPJ", docDest) : new XElement(Ns + "CPF", docDest),
            // Em homologação o nome do destinatário também é padronizado pela SEFAZ.
            new XElement(Ns + "xNome", tpAmb == 2 ? "NF-E EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL" : cliente.Nome),
            new XElement(Ns + "enderDest",
                new XElement(Ns + "xLgr", string.IsNullOrWhiteSpace(cliente.Logradouro) ? "NAO INFORMADO" : cliente.Logradouro),
                new XElement(Ns + "nro", string.IsNullOrWhiteSpace(cliente.Numero) ? "S/N" : cliente.Numero),
                new XElement(Ns + "xBairro", string.IsNullOrWhiteSpace(cliente.Bairro) ? "NAO INFORMADO" : cliente.Bairro),
                new XElement(Ns + "cMun", empresa.CodigoMunicipioIbge), // FALLBACK: ver lacuna no topo
                new XElement(Ns + "xMun", string.IsNullOrWhiteSpace(cliente.Cidade) ? empresa.Cidade : cliente.Cidade),
                new XElement(Ns + "UF", ufDest),
                new XElement(Ns + "CEP", ChaveAcessoNfce.SoNumeros(string.IsNullOrWhiteSpace(cliente.Cep) ? empresa.Cep : cliente.Cep!)),
                new XElement(Ns + "cPais", "1058"),
                new XElement(Ns + "xPais", "BRASIL")),
            new XElement(Ns + "indIEDest", contribuinte ? "1" : "9"),
            contribuinte ? new XElement(Ns + "IE", ChaveAcessoNfce.SoNumeros(cliente.InscricaoEstadual!)) : null);

        var dets = new List<XElement>();
        int n = 1;
        foreach (var item in venda.Itens)
        {
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
                new XElement(Ns + "PIS", new XElement(Ns + "PISNT", new XElement(Ns + "CST", "49"))),
                new XElement(Ns + "COFINS", new XElement(Ns + "COFINSNT", new XElement(Ns + "CST", "49"))));

            dets.Add(new XElement(Ns + "det", new XAttribute("nItem", n++), prod, imposto));
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

        var transp = new XElement(Ns + "transp", new XElement(Ns + "modFrete", "9"));

        // Pagamento: para NF-e B2B usa-se normalmente "sem pagamento" (90) ou os pagamentos reais.
        XElement pag;
        if (venda.Pagamentos.Any())
        {
            pag = new XElement(Ns + "pag", venda.Pagamentos.Select(p =>
                new XElement(Ns + "detPag",
                    new XElement(Ns + "tPag", ((int)p.FormaPagamento).ToString("D2")),
                    new XElement(Ns + "vPag", F(p.Valor, 2)))));
        }
        else
        {
            pag = new XElement(Ns + "pag",
                new XElement(Ns + "detPag",
                    new XElement(Ns + "tPag", "90"),
                    new XElement(Ns + "vPag", "0.00")));
        }

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

    private static string F(decimal v, int casas) => v.ToString("F" + casas, Inv);

    private sealed class StringWriterUtf8 : StringWriter
    {
        public StringWriterUtf8(StringBuilder sb) : base(sb) { }
        public override Encoding Encoding => Encoding.UTF8;
    }
}
