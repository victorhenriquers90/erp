using System.Xml.Linq;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Nfce;
using ProjetoVarejo.Shared;
using Xunit;

namespace ProjetoVarejo.Tests.Nfce;

/// <summary>
/// Valida a estrutura do XML da NFC-e sem precisar de certificado nem conexão com a SEFAZ.
/// Trava as correções do showstopper: infNFeSupl/QR Code obrigatório, ordem do schema,
/// e a frase fixa do 1º item em homologação.
/// </summary>
public class NfceXmlGeneratorTests
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";
    private static readonly XNamespace Dsig = "http://www.w3.org/2000/09/xmldsig#";

    private static EmpresaConfig EmpresaHomologacao() => new()
    {
        Id = 1,
        RazaoSocial = "LOJA TESTE LTDA",
        NomeFantasia = "LOJA TESTE",
        Cnpj = "11.222.333/0001-81",
        InscricaoEstadual = "123456789012",
        Cep = "01001-000",
        Logradouro = "PRACA DA SE",
        Numero = "100",
        Bairro = "SE",
        Cidade = "SAO PAULO",
        Uf = "SP",
        CodigoMunicipioIbge = "3550308",
        RegimeTributario = "1",
        CscId = "1",
        CscToken = "TESTE-CSC-TOKEN-0000000000000000",
        AmbienteHomologacao = true,
        SerieNfce = 1
    };

    private static Produto Prod(string codigo, string descricao) => new()
    {
        Codigo = codigo,
        Descricao = descricao,
        CodigoBarras = "7891234567890",
        Ncm = "22021000",
        Cfop = "5102",
        Origem = "0",
        Unidade = UnidadeMedida.UN
    };

    private static Venda VendaComDoisItens()
    {
        var venda = new Venda
        {
            Id = 1,
            Numero = "1",
            SubTotal = 30m,
            Desconto = 0m,
            Total = 30m,
            Troco = 0m
        };
        venda.Itens.Add(new ItemVenda { Produto = Prod("P1", "REFRIGERANTE LATA"), Quantidade = 1, PrecoUnitario = 10m, Total = 10m });
        venda.Itens.Add(new ItemVenda { Produto = Prod("P2", "AGUA MINERAL"), Quantidade = 2, PrecoUnitario = 10m, Total = 20m });
        venda.Pagamentos.Add(new PagamentoVenda { FormaPagamento = FormaPagamentoTipo.Dinheiro, Valor = 30m });
        return venda;
    }

    [Fact]
    public void GerarXml_EmHomologacao_PrimeiroItemUsaFraseFixa()
    {
        var gen = new NfceXmlGenerator();
        var xml = gen.GerarXml(VendaComDoisItens(), EmpresaHomologacao(), 1, 1, out _, out _);

        var doc = XDocument.Parse(xml);
        var itens = doc.Descendants(Ns + "det").ToList();

        var xProd1 = itens[0].Element(Ns + "prod")!.Element(Ns + "xProd")!.Value;
        var xProd2 = itens[1].Element(Ns + "prod")!.Element(Ns + "xProd")!.Value;

        Assert.Equal("NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL", xProd1);
        Assert.Equal("AGUA MINERAL", xProd2); // demais itens mantêm descrição real
    }

    [Fact]
    public void GerarXml_GeraChaveDe44DigitosEVersao400()
    {
        var gen = new NfceXmlGenerator();
        var xml = gen.GerarXml(VendaComDoisItens(), EmpresaHomologacao(), 1, 1, out var chave, out _);

        Assert.Equal(44, chave.Length);
        Assert.All(chave, c => Assert.True(char.IsDigit(c)));

        var doc = XDocument.Parse(xml);
        var infNFe = doc.Descendants(Ns + "infNFe").Single();
        Assert.Equal("4.00", (string?)infNFe.Attribute("versao"));
        Assert.Equal("NFe" + chave, (string?)infNFe.Attribute("Id"));
    }

    [Fact]
    public void InserirQrCode_AdicionaInfNFeSuplComQrCode()
    {
        var gen = new NfceXmlGenerator();
        var empresa = EmpresaHomologacao();
        var xml = gen.GerarXml(VendaComDoisItens(), empresa, 1, 1, out var chave, out _);

        // Simula o XML assinado: injeta uma <Signature> com DigestValue antes de </NFe>.
        var fakeSignature =
            $"<Signature xmlns=\"{Dsig}\"><SignedInfo><Reference URI=\"#NFe{chave}\">" +
            "<DigestValue>QWJjZGVmMTIzNA==</DigestValue></Reference></SignedInfo>" +
            "<SignatureValue>x</SignatureValue></Signature>";
        var xmlAssinado = xml.Replace("</NFe>", fakeSignature + "</NFe>");

        var final = gen.InserirQrCode(xmlAssinado, empresa, contingencia: false);

        var doc = XDocument.Parse(final);
        var nfe = doc.Root!;

        // infNFeSupl deve existir com qrCode e urlChave
        var supl = nfe.Element(Ns + "infNFeSupl");
        Assert.NotNull(supl);
        var qr = supl!.Element(Ns + "qrCode")!.Value;
        Assert.Contains("chNFe=" + chave, qr);
        Assert.Contains("cIdToken=", qr);
        Assert.Contains("cHashQRCode=", qr);

        Assert.NotNull(supl.Element(Ns + "urlChave"));
    }

    [Fact]
    public void InserirQrCode_RespeitaOrdemDoSchema_InfNFeSuplAntesDaSignature()
    {
        var gen = new NfceXmlGenerator();
        var empresa = EmpresaHomologacao();
        var xml = gen.GerarXml(VendaComDoisItens(), empresa, 1, 1, out var chave, out _);

        var fakeSignature =
            $"<Signature xmlns=\"{Dsig}\"><SignedInfo><Reference URI=\"#NFe{chave}\">" +
            "<DigestValue>QWJjZGVmMTIzNA==</DigestValue></Reference></SignedInfo>" +
            "<SignatureValue>x</SignatureValue></Signature>";
        var xmlAssinado = xml.Replace("</NFe>", fakeSignature + "</NFe>");

        var final = gen.InserirQrCode(xmlAssinado, empresa, contingencia: false);

        // Ordem exigida pelo schema: infNFe → infNFeSupl → Signature
        var posSupl = final.IndexOf("<infNFeSupl", StringComparison.Ordinal);
        var posSig = final.IndexOf("<Signature", StringComparison.Ordinal);
        Assert.True(posSupl > 0, "infNFeSupl não encontrado");
        Assert.True(posSig > 0, "Signature não encontrada");
        Assert.True(posSupl < posSig, "infNFeSupl deve vir ANTES da Signature");
    }

    [Fact]
    public void InserirQrCode_Online_NaoIncluiParametrosDeContingencia()
    {
        var gen = new NfceXmlGenerator();
        var empresa = EmpresaHomologacao();
        var xml = gen.GerarXml(VendaComDoisItens(), empresa, 1, 1, out var chave, out _);
        var xmlAssinado = xml.Replace("</NFe>",
            $"<Signature xmlns=\"{Dsig}\"><SignedInfo><Reference URI=\"#NFe{chave}\">" +
            "<DigestValue>QWJjZGVmMTIzNA==</DigestValue></Reference></SignedInfo>" +
            "<SignatureValue>x</SignatureValue></Signature></NFe>");

        var final = gen.InserirQrCode(xmlAssinado, empresa, contingencia: false);
        var qr = XDocument.Parse(final).Root!.Element(Ns + "infNFeSupl")!.Element(Ns + "qrCode")!.Value;

        // QR online (tpEmis=1) NÃO leva digVal/dhEmi/vNF (são exclusivos da contingência offline)
        Assert.DoesNotContain("digVal=", qr);
        Assert.DoesNotContain("dhEmi=", qr);
    }
}
