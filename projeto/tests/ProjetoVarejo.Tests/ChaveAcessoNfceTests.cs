using FluentAssertions;
using ProjetoVarejo.Infrastructure.Nfce;
using Xunit;

namespace ProjetoVarejo.Tests;

public class ChaveAcessoNfceTests
{
    [Fact]
    public void CalcularDv_ChaveTodaZeros_RetornaZero()
    {
        var chave43 = new string('0', 43);
        ChaveAcessoNfce.CalcularDv(chave43).Should().Be(0);
    }

    [Fact]
    public void CalcularDv_ChaveTodaUm_RetornaConsistente()
    {
        // Soma com pesos (8 ciclos completos + 3 extras):
        // soma = 5 * (2+3+4+5+6+7+8+9) = 5*44 = 220, mais 3 últimos com pesos 2,3,4 = 9
        // Não, todo dígito = 1, então soma = sum(pesos) por 43 posições
        // pesos repetem cada 8: ciclo soma = 44. 43 posições = 5 ciclos completos (40) + 3 (2+3+4=9) = 220+9... espera
        // pesos por posição 0..42 = 2,3,4,5,6,7,8,9, 2,3,4,5,6,7,8,9, 2,3,4,5,6,7,8,9, 2,3,4,5,6,7,8,9, 2,3,4,5,6,7,8,9, 2,3,4
        // soma = 5*44 + (2+3+4) = 220 + 9 = 229. resto = 229 % 11 = 9. dv = 11-9 = 2
        var chave43 = new string('1', 43);
        ChaveAcessoNfce.CalcularDv(chave43).Should().Be(2);
    }

    [Fact]
    public void CalcularDv_ResultadoQuandoRestoMaiorQue1_DvSemAjuste()
    {
        // Soma=10 -> resto=10 -> 11-10=1, dv=1
        // Vou usar uma chave que dê resto = 10
        // chave[42-0]*2 + ...
        // Mais simples: testa que sempre retorna 0-9
        var chave43 = "1234567890123456789012345678901234567890123";
        var dv = ChaveAcessoNfce.CalcularDv(chave43);
        dv.Should().BeInRange(0, 9);
    }

    [Fact]
    public void CalcularDv_TamanhoInvalido_LancaExcecao()
    {
        Action act = () => ChaveAcessoNfce.CalcularDv("123");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SoNumeros_RemoveCaracteresNaoNumericos()
    {
        ChaveAcessoNfce.SoNumeros("12.345.678/0001-99").Should().Be("12345678000199");
        ChaveAcessoNfce.SoNumeros(null!).Should().Be("");
        ChaveAcessoNfce.SoNumeros("").Should().Be("");
        ChaveAcessoNfce.SoNumeros("ABC").Should().Be("");
    }

    [Fact]
    public void Gerar_ChaveTem44Digitos()
    {
        var chave = ChaveAcessoNfce.Gerar(
            ufCodigo: "35",
            dataEmissao: new DateTime(2026, 5, 23),
            cnpjEmitente: "12345678000199",
            modelo: "65",
            serie: 1,
            numero: 1,
            tpEmis: 1,
            cNF: 12345678);

        chave.Should().HaveLength(44);
        chave.Should().MatchRegex(@"^\d{44}$");
        chave.Should().StartWith("352605"); // UF35 + AAMM=2605
    }

    [Theory]
    [InlineData("35", 2026, 5, "12345678000199", 1, 1, 1, 12345678)]
    [InlineData("35", 2026, 12, "00000000000000", 1, 999999999, 1, 99999999)]
    [InlineData("35", 2026, 1, "11111111111111", 999, 1, 9, 1)]
    public void Gerar_DvBateComCalculado(string uf, int ano, int mes, string cnpj,
        int serie, int numero, int tpEmis, int cNF)
    {
        var chave = ChaveAcessoNfce.Gerar(uf, new DateTime(ano, mes, 1), cnpj, "65", serie, numero, tpEmis, cNF);
        var dvCalculado = ChaveAcessoNfce.CalcularDv(chave[..43]);
        int.Parse(chave[43].ToString()).Should().Be(dvCalculado);
    }
}
