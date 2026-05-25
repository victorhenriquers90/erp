using FluentAssertions;
using ProjetoVarejo.Infrastructure.Data;
using Xunit;

namespace ProjetoVarejo.Tests;

public class SenhaHasherTests
{
    [Fact]
    public void Hash_GeraHashesDiferentesParaMesmaSenha()
    {
        var h1 = SenhaHasher.Hash("admin123");
        var h2 = SenhaHasher.Hash("admin123");
        h1.Should().NotBe(h2); // salt aleatório
    }

    [Fact]
    public void Verifica_SenhaCorreta_RetornaTrue()
    {
        var hash = SenhaHasher.Hash("minhaSenha!@#");
        SenhaHasher.Verifica("minhaSenha!@#", hash).Should().BeTrue();
    }

    [Fact]
    public void Verifica_SenhaIncorreta_RetornaFalse()
    {
        var hash = SenhaHasher.Hash("minhaSenha");
        SenhaHasher.Verifica("outraSenha", hash).Should().BeFalse();
        SenhaHasher.Verifica("minhaSenh", hash).Should().BeFalse();
        SenhaHasher.Verifica("", hash).Should().BeFalse();
    }

    [Fact]
    public void Verifica_HashCorrompido_RetornaFalse()
    {
        SenhaHasher.Verifica("qualquer", "lixo").Should().BeFalse();
        SenhaHasher.Verifica("qualquer", "AAA.BBB").Should().BeFalse();
        SenhaHasher.Verifica("qualquer", "").Should().BeFalse();
    }

    [Fact]
    public void Hash_FormatoEsperado()
    {
        var hash = SenhaHasher.Hash("teste");
        var partes = hash.Split('.');
        partes.Should().HaveCount(2);
        Convert.FromBase64String(partes[0]).Should().HaveCount(16); // salt 16 bytes
        Convert.FromBase64String(partes[1]).Should().HaveCount(32); // key 32 bytes
    }
}
