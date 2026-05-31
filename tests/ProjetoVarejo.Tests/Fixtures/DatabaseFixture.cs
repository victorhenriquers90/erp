using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Tests.Builders;

namespace ProjetoVarejo.Tests.Fixtures;

/// <summary>
/// Shared fixture for database setup with seed data.
/// Implements IAsyncLifetime for proper async initialization in xUnit.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private AppDbContext? _dbContext;
    private readonly string _connectionString = "Data Source=:memory:;Cache=Shared";

    /// <summary>
    /// Initialize async - called once before all tests in this fixture.
    /// </summary>
    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        _dbContext = new AppDbContext(options);

        // Create database
        await _dbContext.Database.OpenConnectionAsync();
        await _dbContext.Database.EnsureCreatedAsync();

        // Seed test data
        await SeedTestDataAsync();
    }

    /// <summary>
    /// Dispose async - called once after all tests in this fixture.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.Database.CloseConnectionAsync();
            await _dbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Get database context for direct data access.
    /// </summary>
    public AppDbContext GetContext()
    {
        if (_dbContext == null)
            throw new InvalidOperationException("Database not initialized. Call InitializeAsync first.");
        return _dbContext;
    }

    /// <summary>
    /// Get repository for direct entity access.
    /// </summary>
    public IQueryable<T> GetRepository<T>() where T : class
    {
        return GetContext().Set<T>();
    }

    /// <summary>
    /// Reset database to clean state.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        if (_dbContext == null) return;

        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.Database.EnsureCreatedAsync();
        await SeedTestDataAsync();
    }

    /// <summary>
    /// Seed test data - users, products, categories.
    /// </summary>
    private async Task SeedTestDataAsync()
    {
        if (_dbContext == null) return;

        // Seed Users
        var admin = UsuarioBuilder.CreateAdmin(1);
        var gerente = UsuarioBuilder.CreateGerente(2);
        var caixa = UsuarioBuilder.CreateCaixa(3);
        var estoquista = UsuarioBuilder.CreateEstoquista(4);

        _dbContext.Usuarios.AddRange(admin, gerente, caixa, estoquista);
        await _dbContext.SaveChangesAsync();

        // Seed Categories
        var categories = new List<Categoria>
        {
            new() { Id = 1, Nome = "Eletrônicos", Descricao = "Produtos eletrônicos em geral" },
            new() { Id = 2, Nome = "Alimentos", Descricao = "Alimentos e bebidas" },
            new() { Id = 3, Nome = "Roupas", Descricao = "Vestuário em geral" },
            new() { Id = 4, Nome = "Higiene", Descricao = "Produtos de higiene pessoal" }
        };
        _dbContext.Categorias.AddRange(categories);
        await _dbContext.SaveChangesAsync();

        // Seed Products
        var products = new List<Produto>
        {
            new ProdutoBuilder()
                .WithId(1)
                .WithCodigo("ELETRO001")
                .WithDescricao("Monitor LED 24\"")
                .WithCategoriaId(1)
                .WithPrecoVenda(800m)
                .WithPrecoCusto(500m)
                .WithEstoque(50m)
                .Build(),

            new ProdutoBuilder()
                .WithId(2)
                .WithCodigo("ALIM001")
                .WithDescricao("Arroz 5kg")
                .WithCategoriaId(2)
                .WithPrecoVenda(25m)
                .WithPrecoCusto(15m)
                .WithEstoque(200m)
                .WithPermiteVendaFracionada(true)
                .Build(),

            new ProdutoBuilder()
                .WithId(3)
                .WithCodigo("ROUPA001")
                .WithDescricao("Camiseta Básica P")
                .WithCategoriaId(3)
                .WithPrecoVenda(50m)
                .WithPrecoCusto(20m)
                .WithEstoque(100m)
                .Build(),

            new ProdutoBuilder()
                .WithId(4)
                .WithCodigo("HIG001")
                .WithDescricao("Sabonete Neutro 80g")
                .WithCategoriaId(4)
                .WithPrecoVenda(5m)
                .WithPrecoCusto(2m)
                .WithEstoque(500m)
                .Build(),

            new ProdutoBuilder()
                .WithId(5)
                .WithCodigo("ELETRO002")
                .WithDescricao("Teclado Mecânico RGB")
                .WithCategoriaId(1)
                .WithPrecoVenda(300m)
                .WithPrecoCusto(180m)
                .WithEstoque(25m)
                .Build()
        };
        _dbContext.Produtos.AddRange(products);
        await _dbContext.SaveChangesAsync();

        // Seed Customers
        var customers = new List<Cliente>
        {
            new()
            {
                Id = 1,
                Nome = "Cliente Teste 1",
                CpfCnpj = "12345678901234",
                Email = "cliente1@example.com",
                Telefone = "11999999999",
                PessoaJuridica = true
            },
            new()
            {
                Id = 2,
                Nome = "Cliente Teste 2",
                CpfCnpj = "12345678901235",
                Email = "cliente2@example.com",
                Telefone = "11988888888",
                PessoaJuridica = true
            }
        };
        _dbContext.Clientes.AddRange(customers);
        await _dbContext.SaveChangesAsync();
    }
}
