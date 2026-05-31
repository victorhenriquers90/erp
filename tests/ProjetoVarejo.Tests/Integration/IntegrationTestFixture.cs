using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Api;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Domain.Enums;
using ProjetoVarejo.Shared;
using ProjetoVarejo.Infrastructure.Data;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace ProjetoVarejo.Tests.Integration;

/// <summary>
/// WebApplicationFactory subclass for integration testing with IAsyncLifetime support.
/// Configures test application with in-memory SQLite database, seeds test data, and provides HttpClient.
/// </summary>
public class IntegrationTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString = $"Data Source=:memory:{Guid.NewGuid():N}";
    private AppDbContext? _dbContext;

    public async Task InitializeAsync()
    {
        // Create and seed database
        _dbContext = GetDbContext();
        await _dbContext.Database.EnsureCreatedAsync();
        await SeedTestDataAsync(_dbContext);
    }

    public async Task DisposeAsync()
    {
        if (_dbContext != null)
        {
            await _dbContext.Database.EnsureDeletedAsync();
            await _dbContext.DisposeAsync();
        }
    }

    /// <summary>
    /// Override ConfigureWebHost to setup test database and DI configuration.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove production database registration
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // Add in-memory SQLite for testing
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_connectionString));
        });

        builder.UseEnvironment("Testing");
    }

    /// <summary>
    /// Create HttpClient (use for making API requests without pre-set authorization).
    /// </summary>
    public new HttpClient CreateClient() => base.CreateClient();

    /// <summary>
    /// Create HttpClient with authentication header set to provided JWT token.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string jwtToken)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
        return client;
    }

    /// <summary>
    /// Get DbContext for direct data access in tests.
    /// </summary>
    public AppDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    /// <summary>
    /// Seed test data: users, products, categories, suppliers.
    /// </summary>
    private async Task SeedTestDataAsync(AppDbContext dbContext)
    {
        // Seed test users with different roles
        var adminUser = new Usuario
        {
            Id = 1,
            Login = "admin",
            Nome = "Administrador",
            Perfil = PerfilUsuario.Administrador,
            Ativo = true,
            SenhaHash = HashPassword("senha123")
        };

        var gerenteUser = new Usuario
        {
            Id = 2,
            Login = "gerente",
            Nome = "Gerente",
            Perfil = PerfilUsuario.Gerente,
            Ativo = true,
            SenhaHash = HashPassword("senha123")
        };

        var caixaUser = new Usuario
        {
            Id = 3,
            Login = "caixa",
            Nome = "Caixa",
            Perfil = PerfilUsuario.Caixa,
            Ativo = true,
            SenhaHash = HashPassword("senha123")
        };

        var estoquistaUser = new Usuario
        {
            Id = 4,
            Login = "estoquista",
            Nome = "Estoquista",
            Perfil = PerfilUsuario.Estoquista,
            Ativo = true,
            SenhaHash = HashPassword("senha123")
        };

        await dbContext.Usuarios.AddRangeAsync(adminUser, gerenteUser, caixaUser, estoquistaUser);

        // Seed test category
        var categoria = new Categoria
        {
            Id = 1,
            Nome = "Produtos Teste",
            Descricao = "Categoria para testes de integração"
        };

        await dbContext.Categorias.AddAsync(categoria);

        // Seed test products
        var product = new Produto
        {
            Id = 1,
            Codigo = "PROD001",
            Descricao = "Produto para testes de integração",
            PrecoVenda = 100m,
            PrecoCusto = 50m,
            EstoqueMinimo = 5,
            Estoque = 100m,
            CategoriaId = 1
        };

        var product2 = new Produto
        {
            Id = 2,
            Codigo = "PROD002",
            Descricao = "Segundo produto para testes",
            PrecoVenda = 50m,
            PrecoCusto = 25m,
            EstoqueMinimo = 10,
            Estoque = 200m,
            CategoriaId = 1
        };

        await dbContext.Produtos.AddRangeAsync(product, product2);

        // Seed test supplier
        var supplier = new Fornecedor
        {
            Id = 1,
            RazaoSocial = "Fornecedor Teste",
            Cnpj = "12345678000195",
            Email = "fornecedor@test.com",
            Telefone = "1133334444"
        };

        await dbContext.Fornecedores.AddAsync(supplier);

        // Seed financial account
        var conta = new ContaFinanceira
        {
            Id = 1,
            Descricao = "Conta Corrente Teste",
            DataVencimento = DateTime.Today.AddMonths(1),
            Valor = 1000m
        };

        await dbContext.ContasFinanceiras.AddAsync(conta);

        // Commit all changes
        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Simple PBKDF2-SHA256 password hashing for test data.
    /// </summary>
    private string HashPassword(string senha)
    {
        using (var rng = new RNGCryptoServiceProvider())
        {
            byte[] salt = new byte[16];
            rng.GetBytes(salt);

            var pbkdf2 = new Rfc2898DeriveBytes(
                senha,
                salt,
                100000,
                HashAlgorithmName.SHA256);

            byte[] hash = pbkdf2.GetBytes(20);
            byte[] hashWithSalt = new byte[36];
            Buffer.BlockCopy(salt, 0, hashWithSalt, 0, 16);
            Buffer.BlockCopy(hash, 0, hashWithSalt, 16, 20);

            return Convert.ToBase64String(hashWithSalt);
        }
    }
}
