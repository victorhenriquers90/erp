using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Infrastructure.Repositories;
using System.Windows;

namespace ProjetoVarejo.Desktop.Erp;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory)
                   .AddJsonFile("appsettings.json", optional: true);
            })
            .ConfigureServices((ctx, sc) =>
            {
                var connStr = ctx.Configuration.GetConnectionString("Default")
                    ?? @"Server=.\SQLEXPRESS;Database=ProjetoVarejo;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;";

                sc.AddDbContext<AppDbContext>(opt =>
                    opt.UseSqlServer(connStr, sql => sql.EnableRetryOnFailure(3)));

                sc.AddScoped<IUnitOfWork, UnitOfWork>();
                sc.AddScoped<FornecedorService>();
                sc.AddScoped<ProdutoService>();
                sc.AddScoped<PedidoCompraService>();
                sc.AddTransient<MainWindow>();
            })
            .Build();

        await _host.StartAsync();
        Services = _host.Services;
        await EnsureErpSchemaAsync();

        var main = Services.GetRequiredService<MainWindow>();
        MainWindow = main;
        main.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }

    private static async Task EnsureErpSchemaAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        const string sql = """
IF OBJECT_ID('dbo.PedidosCompra', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[PedidosCompra](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Numero] NVARCHAR(30) NOT NULL,
        [FornecedorId] INT NOT NULL,
        [DataEmissao] DATETIME2 NOT NULL,
        [Status] NVARCHAR(30) NOT NULL,
        [Total] DECIMAL(18,4) NOT NULL,
        [Observacao] NVARCHAR(500) NULL,
        [CriadoEm] DATETIME2 NOT NULL,
        [AtualizadoEm] DATETIME2 NULL,
        [Ativo] BIT NOT NULL
    );
    CREATE UNIQUE INDEX [IX_PedidosCompra_Numero] ON [dbo].[PedidosCompra]([Numero]);
    ALTER TABLE [dbo].[PedidosCompra] ADD CONSTRAINT [FK_PedidosCompra_Fornecedores_FornecedorId]
        FOREIGN KEY([FornecedorId]) REFERENCES [dbo].[Fornecedores]([Id]);
END;

IF OBJECT_ID('dbo.ItensPedidoCompra', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ItensPedidoCompra](
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [PedidoCompraId] INT NOT NULL,
        [ProdutoId] INT NULL,
        [Descricao] NVARCHAR(200) NOT NULL,
        [Quantidade] DECIMAL(18,4) NOT NULL,
        [QuantidadeRecebida] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_ItensPedidoCompra_QuantidadeRecebida] DEFAULT(0),
        [ValorUnitario] DECIMAL(18,4) NOT NULL,
        [ValorUnitarioRecebido] DECIMAL(18,4) NULL,
        [Subtotal] DECIMAL(18,4) NOT NULL,
        [CriadoEm] DATETIME2 NOT NULL,
        [AtualizadoEm] DATETIME2 NULL,
        [Ativo] BIT NOT NULL
    );
    CREATE INDEX [IX_ItensPedidoCompra_PedidoCompraId] ON [dbo].[ItensPedidoCompra]([PedidoCompraId]);
    CREATE INDEX [IX_ItensPedidoCompra_ProdutoId] ON [dbo].[ItensPedidoCompra]([ProdutoId]);
    ALTER TABLE [dbo].[ItensPedidoCompra] ADD CONSTRAINT [FK_ItensPedidoCompra_PedidosCompra_PedidoCompraId]
        FOREIGN KEY([PedidoCompraId]) REFERENCES [dbo].[PedidosCompra]([Id]) ON DELETE CASCADE;
    ALTER TABLE [dbo].[ItensPedidoCompra] ADD CONSTRAINT [FK_ItensPedidoCompra_Produtos_ProdutoId]
        FOREIGN KEY([ProdutoId]) REFERENCES [dbo].[Produtos]([Id]) ON DELETE SET NULL;
END;

IF COL_LENGTH('dbo.ItensPedidoCompra', 'ProdutoId') IS NULL
BEGIN
    ALTER TABLE [dbo].[ItensPedidoCompra] ADD [ProdutoId] INT NULL;
END;

IF COL_LENGTH('dbo.ItensPedidoCompra', 'QuantidadeRecebida') IS NULL
BEGIN
    ALTER TABLE [dbo].[ItensPedidoCompra] ADD [QuantidadeRecebida] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_ItensPedidoCompra_QuantidadeRecebida] DEFAULT(0);
END;

IF COL_LENGTH('dbo.ItensPedidoCompra', 'ValorUnitarioRecebido') IS NULL
BEGIN
    ALTER TABLE [dbo].[ItensPedidoCompra] ADD [ValorUnitarioRecebido] DECIMAL(18,4) NULL;
END;

IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name = 'IX_ItensPedidoCompra_ProdutoId' AND object_id = OBJECT_ID('dbo.ItensPedidoCompra'))
BEGIN
    CREATE INDEX [IX_ItensPedidoCompra_ProdutoId] ON [dbo].[ItensPedidoCompra]([ProdutoId]);
END;

IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ItensPedidoCompra_Produtos_ProdutoId')
BEGIN
    ALTER TABLE [dbo].[ItensPedidoCompra] ADD CONSTRAINT [FK_ItensPedidoCompra_Produtos_ProdutoId]
        FOREIGN KEY([ProdutoId]) REFERENCES [dbo].[Produtos]([Id]) ON DELETE SET NULL;
END;
""";

        await db.Database.ExecuteSqlRawAsync(sql);
    }
}
