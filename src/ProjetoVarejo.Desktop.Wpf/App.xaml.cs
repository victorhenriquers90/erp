using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Desktop.Wpf.ViewModels;
using ProjetoVarejo.Desktop.Wpf.Views;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Infrastructure.Repositories;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using WpfApp = System.Windows.Application;
using WpfExit = System.Windows.ExitEventArgs;
using WpfStartup = System.Windows.StartupEventArgs;

namespace ProjetoVarejo.Desktop.Wpf;

public partial class App : WpfApp
{
    private IHost? _host;
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "app_startup.log");
    private static bool _uiErrorShown;

    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(WpfStartup e)
    {
        base.OnStartup(e);
        global::Wpf.Ui.Appearance.ApplicationThemeManager.Apply(global::Wpf.Ui.Appearance.ApplicationTheme.Light);
        SetupGlobalExceptionHandlers();
        Log("=== APP STARTUP ===");

        try
        {
            _host = BuildHost();
            await _host.StartAsync();
            Services = _host.Services;
            Log("Host iniciado");

            await TryApplyMigrationsAsync();

            var login = Services.GetRequiredService<LoginWindow>();
            MainWindow = login;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            login.Show();
            login.Activate();

            Log("LoginWindow exibida");
        }
        catch (Exception ex)
        {
            Fatal("Erro ao iniciar a aplicacao", ex);
        }
    }

    protected override async void OnExit(WpfExit e)
    {
        Log($"OnExit acionado. ExitCode={e.ApplicationExitCode}");
        try
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log("Erro no encerramento", ex);
        }

        base.OnExit(e);
    }

    private static IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.SetBasePath(AppContext.BaseDirectory)
                   .AddJsonFile("appsettings.json", optional: true)
                   .AddJsonFile("appsettings.Development.json", optional: true);
            })
            .ConfigureServices((ctx, sc) =>
            {
                var connStr = ctx.Configuration.GetConnectionString("Default")
                    ?? @"Server=.\SQLEXPRESS;Database=ProjetoVarejo;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;";

                sc.AddDbContext<AppDbContext>(opt =>
                    opt.UseSqlServer(connStr, sql => sql.EnableRetryOnFailure(3)));

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
            })
            .Build();
    }

    private static async Task TryApplyMigrationsAsync()
    {
        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.MigrateAsync().WaitAsync(TimeSpan.FromSeconds(20));
            Log("Migrations aplicadas");
        }
        catch (Exception ex)
        {
            Log("Falha ao aplicar migrations", ex);
            MessageBox.Show(
                "Nao foi possivel validar o banco no startup. O aplicativo vai continuar, mas pode falhar ao carregar dados.",
                "Aviso de banco",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private static void SetupGlobalExceptionHandlers()
    {
        Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Log("APPDOMAIN EXCEPTION", ex);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log("UNOBSERVED TASK EXCEPTION", args.Exception);
            args.SetObserved();
        };
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log("DISPATCHER EXCEPTION", e.Exception);
        if (!_uiErrorShown)
        {
            _uiErrorShown = true;
            MessageBox.Show(
                $"Erro inesperado na interface:\n{e.Exception.Message}\n\nDetalhes em:\n{LogPath}",
                "Erro de interface",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        // Nao mascarar excecoes de UI: evita tela ficar em estado corrompido.
        e.Handled = false;
    }

    private static void Fatal(string message, Exception ex)
    {
        Log(message, ex);
        MessageBox.Show(
            $"{message}\n\n{ex.GetType().Name}: {ex.Message}\n\nDetalhes em:\n{LogPath}",
            "Erro fatal",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        Current.Shutdown(-1);
    }

    private static void Log(string message, Exception? ex = null)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var text = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            if (ex != null)
            {
                text += $"{Environment.NewLine}{ex}";
            }

            File.AppendAllText(LogPath, text + Environment.NewLine);
        }
        catch
        {
            // sem throw no logger de startup
        }
    }
}
