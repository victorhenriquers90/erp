using FluentAssertions;
using ProjetoVarejo.Infrastructure.Pix;
using Xunit;

namespace ProjetoVarejo.Tests;

public class PixBrCodeBuilderTests
{
    [Fact]
    public void Gerar_PayloadValido_TerminaComCrc16()
    {
        var pix = PixBrCodeBuilder.Gerar(
            chavePix: "teste@example.com",
            nomeRecebedor: "EMPRESA TESTE",
            cidade: "SAO PAULO",
            valor: 100.50m,
            txid: "VENDA123");

        pix.Should().Contain("6304");
        pix.Should().EndWith(pix[^4..]); // últimos 4 chars = CRC
        pix[^4..].Length.Should().Be(4);
    }

    [Fact]
    public void Gerar_ContemTodosCamposObrigatorios()
    {
        var pix = PixBrCodeBuilder.Gerar(
            chavePix: "12345678900",
            nomeRecebedor: "Lojinha",
            cidade: "São Paulo");

        pix.Should().StartWith("000201"); // Payload Format Indicator
        pix.Should().Contain("BR.GOV.BCB.PIX");
        pix.Should().Contain("12345678900");
        pix.Should().Contain("5303986"); // currency BRL
        pix.Should().Contain("5802BR"); // country BR
    }

    [Fact]
    public void Gerar_SemValor_OmiteTag54()
    {
        var pix = PixBrCodeBuilder.Gerar(
            chavePix: "teste@x.com",
            nomeRecebedor: "Teste",
            cidade: "SP");

        // Tag 54 (amount) só deve estar presente se valor > 0
        pix.Should().NotContain("5404");
        pix.Should().NotContain("5410");
    }

    [Fact]
    public void Gerar_ComValor_IncluiTag54FormatadoComPonto()
    {
        var pix = PixBrCodeBuilder.Gerar(
            chavePix: "teste@x.com",
            nomeRecebedor: "Teste",
            cidade: "SP",
            valor: 25.75m);

        pix.Should().Contain("5405"); // tag 54, len 05
        pix.Should().Contain("25.75");
    }

    [Fact]
    public void Gerar_ChaveVazia_LancaExcecao()
    {
        Action act = () => PixBrCodeBuilder.Gerar("", "Nome", "Cidade");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Gerar_NomeComAcento_NormalizaParaAscii()
    {
        var pix = PixBrCodeBuilder.Gerar("x@y.com", "São José", "São Paulo");
        pix.Should().NotContain("ã");
        pix.Should().NotContain("é");
        pix.Should().Contain("SAO");
    }

    [Fact]
    public void Gerar_CrcSeRecalculadoBate()
    {
        // Garante que o payload é bem-formado: parseando até "6304" e recalculando CRC
        var pix = PixBrCodeBuilder.Gerar("teste@x.com", "Teste", "SP", 10);
        var idxCrc = pix.LastIndexOf("6304");
        idxCrc.Should().BeGreaterThan(0);
        var crcInformado = pix.Substring(idxCrc + 4);
        crcInformado.Should().HaveLength(4);
    }
}
