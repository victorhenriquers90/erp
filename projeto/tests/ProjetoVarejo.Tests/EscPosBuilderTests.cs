using FluentAssertions;
using ProjetoVarejo.Infrastructure.Printing;
using Xunit;

namespace ProjetoVarejo.Tests;

public class EscPosBuilderTests
{
    [Fact]
    public void Build_IniciaComEscInit()
    {
        var b = new EscPosBuilder();
        var bytes = b.Build();
        bytes.Length.Should().BeGreaterThan(0);
        bytes[0].Should().Be(0x1B); // ESC
        bytes[1].Should().Be((byte)'@'); // @ (init)
    }

    [Fact]
    public void Cortar_AdicionaSequenciaCorte()
    {
        var b = new EscPosBuilder().Cortar();
        var bytes = b.Build();
        bytes.Length.Should().BeGreaterOrEqualTo(3);
        var ultimos3 = bytes[^3..];
        ultimos3[0].Should().Be(0x1D); // GS
        ultimos3[1].Should().Be((byte)'V');
        ultimos3[2].Should().Be(1);
    }

    [Fact]
    public void Centro_AdicionaComandoAlinhamento()
    {
        var b = new EscPosBuilder().Centro();
        var bytes = b.Build();
        bytes.Should().Contain(new byte[] { 0x1B, (byte)'a', 1 });
    }

    [Fact]
    public void Negrito_OnEOff()
    {
        var bOn = new EscPosBuilder().Negrito(true).Build();
        var bOff = new EscPosBuilder().Negrito(false).Build();
        bOn.Should().Contain(new byte[] { 0x1B, (byte)'E', 1 });
        bOff.Should().Contain(new byte[] { 0x1B, (byte)'E', 0 });
    }

    [Fact]
    public void Linha_AdicionaLineFeed()
    {
        var b = new EscPosBuilder().Linha("teste");
        var bytes = b.Build();
        bytes[^1].Should().Be(0x0A); // LF
    }
}
