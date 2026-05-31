using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Tests;

public class UsuarioServiceTests
{
    [Fact]
    public async Task SalvarAsync_CriaUsuarioComHashDeSenha()
    {
        using var f = new TestDbFactory();
        var svc = new UsuarioService(f.Db);

        var res = await svc.SalvarAsync(new Usuario
        {
            Login = "CAIXA1",
            Nome = "Caixa 1",
            Perfil = PerfilUsuario.Caixa,
            Ativo = true
        }, "senha123");

        Assert.True(res.Sucesso);
        Assert.NotEqual("senha123", res.Valor!.SenhaHash);
        Assert.True(SenhaHasher.Verifica("senha123", res.Valor.SenhaHash));
        Assert.Equal("caixa1", res.Valor.Login);
    }

    [Fact]
    public async Task SalvarAsync_BloqueiaLoginDuplicado()
    {
        using var f = new TestDbFactory();
        var svc = new UsuarioService(f.Db);

        await svc.SalvarAsync(new Usuario { Login = "gerente", Nome = "Gerente", Perfil = PerfilUsuario.Gerente, Ativo = true }, "senha123");
        var res = await svc.SalvarAsync(new Usuario { Login = "GERENTE", Nome = "Outro", Perfil = PerfilUsuario.Caixa, Ativo = true }, "senha123");

        Assert.False(res.Sucesso);
    }

    [Fact]
    public async Task AlternarAtivoAsync_BloqueiaInativarUltimoAdministrador()
    {
        using var f = new TestDbFactory();
        var svc = new UsuarioService(f.Db);
        var adminId = f.Sessao.UsuarioLogado!.Id;

        var res = await svc.AlternarAtivoAsync(adminId);

        Assert.False(res.Sucesso);
        Assert.True(f.Db.Usuarios.First(u => u.Id == adminId).Ativo);
    }

    [Fact]
    public async Task RedefinirSenhaAsync_AtualizaHash()
    {
        using var f = new TestDbFactory();
        var svc = new UsuarioService(f.Db);
        var adminId = f.Sessao.UsuarioLogado!.Id;

        var res = await svc.RedefinirSenhaAsync(adminId, "nova123");

        Assert.True(res.Sucesso);
        Assert.True(SenhaHasher.Verifica("nova123", f.Db.Usuarios.First(u => u.Id == adminId).SenhaHash));
    }
}
