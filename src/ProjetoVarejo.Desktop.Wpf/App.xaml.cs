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
using System.Windows;
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

        // Tenta salvar o log em um local acessível
        var logPath = System.IO.Path.Combine(
            AppContext.BaseDirectory,  // Pasta do executável
            "app_startup.log");

        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath) ?? "");

        void LogError(string msg, Exception? ex = null)
        {
            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}\n";
            if (ex != null)
                text += $"Exception: {ex.GetType().Name}\nMessage: {ex.Message}\nStackTrace: {ex.StackTrace}\n";

            System.IO.File.AppendAllText(logPath, text);
        }

        try { LogError("=== APP STARTUP ==="); } catch { }

        try
        {
            LogError("Iniciando DI Container...");
            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(cfg =>
                {
                    cfg.SetBasePath(AppContext.BaseDirectory)
                       .AddJsonFile("appsettings.json",             optional: true)
                       .AddJsonFile("appsettings.Development.json", optional: true);
                })
                .ConfigureServices((ctx, sc) =>
                {
                    LogError("Configurando services...");
                    var connStr = ctx.Configuration.GetConnectionString("Default")
                        ?? @"Server=.\SQLEXPRESS;Database=ProjetoVarejo;Trusted_Connection=True;TrustServerCertificate=True;";

                    LogError($"Connection string: {connStr}");

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

                    sc.AddTransient<LoginWindow>();
                    sc.AddTransient<MainWindow>();

                    LogError("Services configurados com sucesso");
                })
                .Build();

            LogError("DI Container criado");
            await _host.StartAsync();
            LogError("Host iniciado");

            Services = _host.Services;

            using (var scope = Services.CreateScope())
            {
                LogError("Aplicando migrations...");
                LogError("Obtendo AppDbContext...");
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                LogError("AppDbContext obtido, iniciando MigrateAsync...");

                try
                {
                    // Timeout de 10 segundos para migrations
                    using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        await db.Database.MigrateAsync(cts.Token);
                        LogError("Migrations aplicadas com sucesso");
                    }
                }
                catch (OperationCanceledException)
                {
                    LogError("TIMEOUT: Migrations demorando muito (>10s), prosseguindo mesmo assim");
                }
                catch (Exception ex)
                {
                    LogError("ERRO nas migrations (mas prosseguindo)", ex);
                }
            }

            LogError("Obtendo LoginWindow do DI...");
            var login = Services.GetRequiredService<LoginWindow>();
            LogError("Mostrando LoginWindow...");
            login.Show();
            LogError("LoginWindow mostrada com sucesso");

            await Task.Delay(100); // Dar um tempo para a window aparecer
            LogError("=== APP STARTUP COMPLETO ===");
        }
        catch (Exception ex)
        {
            LogError("ERRO NO STARTUP", ex);
            System.Diagnostics.Debug.WriteLine($"STARTUP ERROR: {ex}");
            MessageBox.Show($"Erro ao iniciar aplicação:\n\n{ex.GetType().Name}: {ex.Message}\n\nVer log em:\n{logPath}",
                "Erro de Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            LogError("APPDOMAIN UNHANDLED EXCEPTION", ex.ExceptionObject as Exception);
            System.Diagnostics.Debug.WriteLine($"UNHANDLED EXCEPTION: {ex.ExceptionObject}");
            MessageBox.Show($"Erro fatal:\n\n{ex.ExceptionObject}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, ex) =>
        {
            LogError("DISPATCHER EXCEPTION", ex.Exception);
            System.Diagnostics.Debug.WriteLine($"DISPATCHER EXCEPTION: {ex.Exception}");
            MessageBox.Show($"Erro na UI:\n\n{ex.Exception.Message}\n\n{ex.Exception.InnerException?.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true;
        };
    }

    protected override async void OnExit(WpfExit e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
