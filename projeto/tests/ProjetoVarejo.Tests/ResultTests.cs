using FluentAssertions;
using ProjetoVarejo.Shared;
using Xunit;

namespace ProjetoVarejo.Tests;

public class ResultTests
{
    [Fact]
    public void Ok_SemValor_ResultadoSucesso()
    {
        var r = Result.Ok();
        r.Sucesso.Should().BeTrue();
        r.Erro.Should().BeNull();
    }

    [Fact]
    public void Falha_ComMensagem_ResultadoFalha()
    {
        var r = Result.Falha("erro");
        r.Sucesso.Should().BeFalse();
        r.Erro.Should().Be("erro");
    }

    [Fact]
    public void Ok_ComValor_GuardaValor()
    {
        var r = Result.Ok(42);
        r.Sucesso.Should().BeTrue();
        r.Valor.Should().Be(42);
    }

    [Fact]
    public void Falha_Generico_ValorIsDefault()
    {
        var r = Result.Falha<string>("erro");
        r.Sucesso.Should().BeFalse();
        r.Valor.Should().BeNull();
        r.Erro.Should().Be("erro");
    }
}
