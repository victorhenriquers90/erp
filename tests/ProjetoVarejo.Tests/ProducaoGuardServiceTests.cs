using Microsoft.Extensions.Configuration;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Tests;

public class ProducaoGuardServiceTests
{
    [Fact]
    public async Task ValidarNfceAsync_BloqueiaProdutoSemNcm()
    {
        using var f = new TestDbFactory();
        using var cert = ArquivoTemporario();
        AdicionarEmpresa(f, cert.Path, producao: true);
        var produto = f.AdicionarProduto("P001", estoque: 10, preco: 10);
        var vendaSvc = NewVendaService(f);
        var venda = (await vendaSvc.NovaVendaAsync()).Valor!;
        await vendaSvc.AdicionarItemAsync(venda.Id, produto.Id, 1);
        var guard = NewGuard(f);

        var res = await guard.ValidarNfceAsync(venda.Id);

        Assert.False(res.PodeContinuar);
        Assert.Contains(res.Bloqueios, p => p.Titulo.Contains("NCM"));
    }

    [Fact]
    public async Task ValidarNfceAsync_AprovaVendaComFiscalBasico()
    {
        using var f = new TestDbFactory();
        using var cert = ArquivoTemporario();
        AdicionarEmpresa(f, cert.Path, producao: true);
        var produto = f.AdicionarProduto("P001", estoque: 10, preco: 10);
        produto.Ncm = "12345678";
        f.Db.SaveChanges();
        var vendaSvc = NewVendaService(f);
        var venda = (await vendaSvc.NovaVendaAsync()).Valor!;
        await vendaSvc.AdicionarItemAsync(venda.Id, produto.Id, 1);
        var guard = NewGuard(f);

        var res = await guard.ValidarNfceAsync(venda.Id);

        Assert.True(res.PodeContinuar);
    }

    [Fact]
    public async Task ValidarPdvAsync_EmProducaoBloqueiaSenhaPadrao()
    {
        using var f = new TestDbFactory();
        using var cert = ArquivoTemporario();
        AdicionarEmpresa(f, cert.Path, producao: true);
        var produto = f.AdicionarProduto("P001", estoque: 10, preco: 10);
        produto.Ncm = "12345678";
        f.Sessao.UsuarioLogado!.SenhaHash = SenhaHasher.Hash("admin");
        f.Db.SaveChanges();
        var guard = NewGuard(f);

        var res = await guard.ValidarPdvAsync();

        Assert.False(res.PodeContinuar);
        Assert.Contains(res.Bloqueios, p => p.Titulo.Contains("Senha"));
    }

    private static VendaService NewVendaService(TestDbFactory f) =>
        new(f.Db, f.Sessao, new EstoqueService(f.Db, f.Sessao));

    private static ProducaoGuardService NewGuard(TestDbFactory f)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "DataSource=:memory:"
            })
            .Build();
        return new ProducaoGuardService(f.Db, config, f.Sessao);
    }

    private static void AdicionarEmpresa(TestDbFactory f, string certPath, bool producao)
    {
        f.Db.EmpresaConfigs.Add(new EmpresaConfig
        {
            RazaoSocial = "MERCADO TESTE LTDA",
            NomeFantasia = "Mercado Teste",
            Cnpj = "12345678000195",
            InscricaoEstadual = "123456789",
            Cep = "01001000",
            Logradouro = "Rua Teste",
            Numero = "100",
            Bairro = "Centro",
            Cidade = "Sao Paulo",
            Uf = "SP",
            CodigoMunicipioIbge = "3550308",
            CertificadoCaminho = certPath,
            CertificadoSenha = "senha",
            CscId = "1",
            CscToken = "TOKEN-CSC-VALIDO-PARA-TESTE-123",
            AmbienteHomologacao = !producao,
            SerieNfce = 1,
            ProximoNumeroNfce = 1,
            ImpressoraDestino = "Printer"
        });
        f.Db.SaveChanges();
    }

    private static TempFile ArquivoTemporario()
    {
        var path = Path.GetTempFileName();
        File.WriteAllText(path, "cert");
        return new TempFile(path);
    }

    private sealed class TempFile : IDisposable
    {
        public TempFile(string path) => Path = path;
        public string Path { get; }
        public void Dispose()
        {
            try { File.Delete(Path); } catch { }
        }
    }
}
