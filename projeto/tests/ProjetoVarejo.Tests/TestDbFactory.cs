using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Tests;

public class TestDbFactory : IDisposable
{
    public AppDbContext Db { get; }
    public SessaoApp Sessao { get; }
    private readonly SqliteConnection _conn;

    public TestDbFactory()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        _conn.Open();
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_conn).Options;
        Db = new AppDbContext(opts);
        Db.Database.EnsureCreated();

        Sessao = new SessaoApp();
        var admin = new Usuario { Login = "admin", Nome = "Admin", SenhaHash = "x", Perfil = PerfilUsuario.Administrador };
        Db.Usuarios.Add(admin);
        Db.SaveChanges();
        Sessao.DefinirUsuario(admin);
    }

    public Produto AdicionarProduto(string codigo, decimal estoque = 10, decimal preco = 10, bool controla = true)
    {
        var p = new Produto
        {
            Codigo = codigo,
            Descricao = "Produto " + codigo,
            PrecoVenda = preco,
            Estoque = estoque,
            ControlaEstoque = controla
        };
        Db.Produtos.Add(p);
        Db.SaveChanges();
        return p;
    }

    public void Dispose()
    {
        Db.Dispose();
        _conn.Dispose();
    }
}
