using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Enums;

namespace ProjetoVarejo.Tests;

public class ImplantacaoServiceTests
{
    [Fact]
    public async Task ObterAsync_SemArquivo_UsaIndustriaComModulosEssenciais()
    {
        var arquivo = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var svc = new ImplantacaoService(arquivo);

        var config = await svc.ObterAsync();

        Assert.Equal(TipoNegocio.Industria, config.Perfil);
        Assert.True((config.ModulosAtivos & ModuloSistema.PDV) != 0, "PDV deve estar ativo");
        Assert.True((config.ModulosAtivos & ModuloSistema.Backup) != 0, "Backup deve estar ativo");
        Assert.True((config.ModulosAtivos & ModuloSistema.Auditoria) != 0, "Auditoria deve estar ativa");
    }

    [Fact]
    public void ModulosRecomendados_Industria_IncluiEstoque()
    {
        var svc = new ImplantacaoService(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json"));

        var modulos = svc.ModulosRecomendados(TipoNegocio.Industria);

        Assert.True((modulos & ModuloSistema.PDV) != 0, "Indústria deve incluir PDV");
        Assert.True((modulos & ModuloSistema.Estoque) != 0, "Indústria deve incluir Estoque");
        Assert.True((modulos & ModuloSistema.Fiscal) != 0, "Indústria deve incluir Fiscal");
    }

    [Fact]
    public async Task SalvarAsync_NormalizaModulosEssenciais()
    {
        var arquivo = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var svc = new ImplantacaoService(arquivo);

        await svc.SalvarAsync(new ImplantacaoConfig
        {
            Perfil = TipoNegocio.Bazar,
            ModulosAtivos = ModuloSistema.PDV
        });

        var config = await svc.ObterAsync();

        Assert.Equal(TipoNegocio.Bazar, config.Perfil);
        Assert.True((config.ModulosAtivos & ModuloSistema.PDV) != 0, "PDV deve estar ativo");
        Assert.True((config.ModulosAtivos & ModuloSistema.Backup) != 0, "Backup deve estar ativo (essencial)");
        Assert.True((config.ModulosAtivos & ModuloSistema.Auditoria) != 0, "Auditoria deve estar ativa (essencial)");
    }
}
