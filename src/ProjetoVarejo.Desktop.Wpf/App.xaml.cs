using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Services;
using ProjetoVarejo.Desktop.Wpf.ViewModels;
using ProjetoVarejo.Desktop.Wpf.Views;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Infrastructure.Repositories;
using WpfApp = System.Windows.Application;
using WpfStartup = System.Windows.StartupEventArgs;
using WpfExit = System.Windows.ExitEventArgs;

namespace ProjetoVarejo.Desktop.Wpf;

public partial class App : WpfApp
{
    private IHost _host = null!;
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(WpfStartup e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory)
                   .AddJsonFile("appsettings.json",             optional: true)
                   .AddJsonFile("appsettings.Development.json", optional: true);
            })
            .ConfigureServices((ctx, sc) =>
            {
                var connStr = ctx.Configuration.GetConnectionString("Default")
                    ?? @"Server=.\SQLEXPRESS;Database=ProjetoVarejo;Trusted_Connection=True;TrustServerCertificate=True;";

                sc.AddDbContext<AppDbContext>(opt =>
                    opt.UseSqlServer(connStr,
                        sql => sql.EnableRetryOnFailure(3)));

                sc.AddScoped<IUnitOfWork, UnitOfWork>();
                sc.AddScoped<IAutenticacaoService, AutenticacaoService>();
                sc.AddScoped<IProdutoService, ProdutoService>();
                sc.AddScoped<IEstoqueService, EstoqueService>();
                sc.AddScoped<IFinanceiroService, FinanceiroService>();
                sc.AddScoped<ICaixaService, CaixaService>();
                sc.AddScoped<IVendaService, VendaService>();
                sc.AddScoped<IRelatorioService, RelatorioService>();

                sc.AddScoped<IValidator<CaixaSessao>, CaixaSessionValidator>();
                sc.AddScoped<IValidator<Venda>, VendaValidator>();
                sc.AddScoped<IValidator<ItemVenda>, ItemVendaValidator>();
                sc.AddScoped<IValidator<PagamentoVenda>, PagamentoVendaValidator>();
                sc.AddScoped<IValidator<Cliente>, ClienteValidator>();
                sc.AddScoped<IValidator<Fornecedor>, FornecedorValidator>();

                sc.AddScoped<ClienteService>();
                sc.AddScoped<ProdutoService>();
                sc.AddScoped<FornecedorService>();
                sc.AddScoped<CategoriaService>();
                sc.AddScoped<UsuarioService>();
                sc.AddScoped<EstoqueService>();
                sc.AddScoped<FinanceiroService>();
                sc.AddScoped<CaixaService>();
                sc.AddScoped<VendaService>();
                sc.AddScoped<RelatorioService>();
                sc.AddScoped<FilialService>();
                sc.AddScoped<AuditLogService>();
                sc.AddSingleton<SessaoApp>();

                // ViewModels
                sc.AddTransient<LoginViewModel>();
                sc.AddTransient<MainViewModel>();
                sc.AddTransient<DashboardViewModel>();
                sc.AddTransient<ClientesViewModel>();
                sc.AddTransient<ProdutosViewModel>();
                sc.AddTransient<FornecedoresViewModel>();
                sc.AddTransient<EstoqueViewModel>();
                sc.AddTransient<FinanceiroViewModel>();
                sc.AddTransient<VendasViewModel>();
                sc.AddTransient<CaixaViewModel>();
                sc.AddTransient<UsuariosViewModel>();
                sc.AddTransient<FiliaisViewModel>();
                sc.AddTransient<RelatoriosViewModel>();
                sc.AddTransient<AuditoriaViewModel>();
                sc.AddTransient<FiscalViewModel>();
                sc.AddTransient<OperacoesViewModel>();
                sc.AddTransient<AdministracaoSistemaViewModel>();

                // Views
                sc.AddTransient<LoginWindow>();
                sc.AddTransient<MainWindow>();
            })
            .Build();

        await _host.StartAsync();
        Services = _host.Services;

        // Garante migrações aplicadas
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync();
        }

        var login = Services.GetRequiredService<LoginWindow>();
        login.Show();
    }

    protected override async void OnExit(WpfExit e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
