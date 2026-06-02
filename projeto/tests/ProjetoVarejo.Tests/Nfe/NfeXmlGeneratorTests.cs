using System.Xml.Linq;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Nfe;
using ProjetoVarejo.Shared;
using Xunit;

namespace ProjetoVarejo.Tests.Nfe;

/// <summary>
/// Valida a estrutura do XML da NF-e (modelo 55) sem certificado nem SEFAZ.
/// Pega erros de montagem (destinatário, modelo, idDest, ausência de QR) antes do teste real.
/// </summary>
public class NfeXmlGeneratorTests
{
    private static readonly XNamespace Ns = "http://www.portalfiscal.inf.br/nfe";

    private static EmpresaConfig Empresa(bool homologacao = true) => new()
    {
        Id = 1,
        RazaoSocial = "INDUSTRIA EXEMPLO LTDA",
        NomeFantasia = "Industria Exemplo",
        Cnpj = "11.222.333/0001-81",
        InscricaoEstadual = "123456789012",
        Cep = "01001-000",
        Logradouro = "Av. Paulista",
        Numero = "1000",
        Bairro = "Bela Vista",
        Cidade = "SAO PAULO",
        Uf = "SP",
        CodigoMunicipioIbge = "3550308",
        RegimeTributario = "1",
        AmbienteHomologacao = homologacao,
        SerieNfe = 1
    };

    private static Cliente ClientePj(string uf = "SP", string? ibge = "3550308", bool contribuinte = true) => new()
    {
        Id = 10,
        Nome = "COMERCIO DESTINO LTDA",
        CpfCnpj = "44.555.666/0001-54",
        PessoaJuridica = true,
        InscricaoEstadual = contribuinte ? "987654321098" : null,
        Logradouro = "Rua das Flores",
        Numero = "250",
        Bairro = "Centro",
        Cidade = "SAO PAULO",
        Uf = uf,
        CodigoMunicipioIbge = ibge
    };

    private static Produto Prod(string cod, string desc) => new()
    {
        Codigo = cod, Descricao = desc, CodigoBarras = "7891000100103",
        Ncm = "22021000", Cfop = "5102", Origem = "0", Unidade = UnidadeMedida.UN
    };

    private static Venda Venda(Cliente? cliente)
    {
        var v = new Venda { Id = 1, Numero = "1", SubTotal = 300m, Total = 300m, Cliente = cliente, ClienteId = cliente?.Id };
        v.Itens.Add(new ItemVenda { Produto = Prod("P1", "PRODUTO A"), Quantidade = 2, PrecoUnitario = 100m, Total = 200m });
        v.Itens.Add(new ItemVenda { Produto = Prod("P2", "PRODUTO B"), Quantidade = 1, PrecoUnitario = 100m, Total = 100m });
        return v;
    }

    [Fact]
    public void GerarXml_Modelo55_ComDestinatarioObrigatorio()
    {
        var gen = new NfeXmlGenerator();
        var xml = gen.GerarXml(Venda(ClientePj()), Empresa(), 1, 1, out var chave, out _);
        var doc = XDocument.Parse(xml);
        var infNFe = doc.Descendants(Ns + "infNFe").Single();

        Assert.Equal(44, chave.Length);
        Assert.Equal("4.00", (string?)infNFe.Attribute("versao"));
        Assert.Equal("55", (string?)infNFe.Element(Ns + "ide")!.Element(Ns + "mod"));

        var dest = infNFe.Element(Ns + "dest");
        Assert.NotNull(dest);
        Assert.NotNull(dest!.Element(Ns + "CNPJ"));
        Assert.NotNull(dest.Element(Ns + "enderDest")!.Element(Ns + "cMun"));
        Assert.Equal("1", (string?)dest.Element(Ns + "indIEDest")); // contribuinte
        Assert.NotNull(dest.Element(Ns + "IE"));
    }

    [Fact]
    public void GerarXml_NaoTemQrCode_NemInfNFeSupl()
    {
        var gen = new NfeXmlGenerator();
        var xml = gen.GerarXml(Venda(ClientePj()), Empresa(), 1, 1, out _, out _);

        Assert.DoesNotContain("infNFeSupl", xml);
        Assert.DoesNotContain("qrCode", xml);
    }

    [Fact]
    public void GerarXml_IdDest_InternaQuandoMesmaUf_InterestadualQuandoDiferente()
    {
        var gen = new NfeXmlGenerator();

        var interna = XDocument.Parse(gen.GerarXml(Venda(ClientePj(uf: "SP")), Empresa(), 1, 1, out _, out _));
        var inter = XDocument.Parse(gen.GerarXml(Venda(ClientePj(uf: "RJ")), Empresa(), 2, 1, out _, out _));

        Assert.Equal("1", (string?)interna.Descendants(Ns + "idDest").Single());
        Assert.Equal("2", (string?)inter.Descendants(Ns + "idDest").Single());
    }

    [Fact]
    public void GerarXml_EmHomologacao_PadronizaPrimeiroItemEDestinatario()
    {
        var gen = new NfeXmlGenerator();
        var xml = gen.GerarXml(Venda(ClientePj()), Empresa(homologacao: true), 1, 1, out _, out _);
        var doc = XDocument.Parse(xml);

        var xProd1 = doc.Descendants(Ns + "det").First().Element(Ns + "prod")!.Element(Ns + "xProd")!.Value;
        var xNomeDest = doc.Descendants(Ns + "dest").Single().Element(Ns + "xNome")!.Value;

        Assert.Equal("NOTA FISCAL EMITIDA EM AMBIENTE DE HOMOLOGACAO - SEM VALOR FISCAL", xProd1);
        Assert.Contains("HOMOLOGACAO", xNomeDest);
    }

    [Fact]
    public void GerarXml_SemCliente_LancaExcecao()
    {
        var gen = new NfeXmlGenerator();
        Assert.Throws<InvalidOperationException>(() => gen.GerarXml(Venda(null), Empresa(), 1, 1, out _, out _));
    }

    [Fact]
    public void GerarXml_NaoContribuinte_IndIEDest9()
    {
        var gen = new NfeXmlGenerator();
        var xml = gen.GerarXml(Venda(ClientePj(contribuinte: false)), Empresa(), 1, 1, out _, out _);
        var dest = XDocument.Parse(xml).Descendants(Ns + "dest").Single();

        Assert.Equal("9", (string?)dest.Element(Ns + "indIEDest"));
        Assert.Null(dest.Element(Ns + "IE"));
    }
}
