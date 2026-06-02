using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Auditing;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Desktop.Wpf.Views;
using ProjetoVarejo.Infrastructure.Backup;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Infrastructure.Nfce;

namespace ProjetoVarejo.Desktop.Wpf;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(AjustarJanelaNaTela));
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"Erro inesperado:\n\n{args.Exception.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var sc = new ServiceCollection();
            sc.AddSingleton<IConfiguration>(config);
            sc.AddSingleton<SessaoApp>();
            sc.AddScoped<AuditSaveChangesInterceptor>();
            sc.AddDbContext<AppDbContext>((sp, opt) =>
                opt.UseSqlServer(config.GetConnectionString("Default"))
                    .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

            sc.AddScoped<AutenticacaoService>();
            sc.AddScoped<ProdutoService>();
            sc.AddScoped<ClienteService>();
            sc.AddScoped<FornecedorService>();
            sc.AddScoped<CategoriaService>();
            sc.AddScoped<EstoqueService>();
            sc.AddScoped<VendaService>();
            sc.AddScoped<FinanceiroService>();
            sc.AddScoped<CaixaService>();
            sc.AddScoped<RelatorioService>();
            sc.AddSingleton<CupomPrinterService>();
            sc.AddScoped<BackupService>();
            sc.AddScoped<NfeImporterService>();
            sc.AddScoped<PermissaoService>();
            sc.AddScoped<AuditLogService>();
            sc.AddSingleton<NfceXmlGenerator>();
            sc.AddSingleton<NfceAssinador>();
            sc.AddSingleton<SefazSpClient>();
            sc.AddSingleton<NfceCancelamentoBuilder>();
            sc.AddSingleton<NfceInutilizacaoBuilder>();
            sc.AddScoped<NfceService>();

            sc.AddTransient<LoginWindow>();
            sc.AddTransient<MainWindow>();
            sc.AddTransient<ClientesWindow>();
            sc.AddTransient<ClienteEditorWindow>();
            sc.AddTransient<ProdutosWindow>();
            sc.AddTransient<ProdutoEditorWindow>();
            sc.AddTransient<FornecedoresWindow>();
            sc.AddTransient<FornecedorEditorWindow>();
            sc.AddTransient<FiliaisWindow>();
            sc.AddTransient<EmpresaEditorWindow>();
            sc.AddTransient<EstoqueWindow>();
            sc.AddTransient<LancamentoEstoqueWindow>();
            sc.AddTransient<FinanceiroWindow>();
            sc.AddTransient<ContaEditorWindow>();
            sc.AddTransient<QuitacaoContaWindow>();
            sc.AddTransient<RelatoriosWindow>();
            sc.AddTransient<RelatorioDetalhesTecnicosWindow>();
            sc.AddTransient<AuditoriaWindow>();
            sc.AddTransient<AuditoriaDetalhesWindow>();
            sc.AddTransient<PedidosWindow>();
            sc.AddTransient<ProducaoWindow>();
            sc.AddTransient<ProjetosWindow>();
            sc.AddTransient<ForcaTrabalhoWindow>();
            sc.AddTransient<ApontamentoHorasWindow>();
            sc.AddTransient<CadeiaSuprimentosWindow>();
            sc.AddTransient<EcommerceWindow>();
            sc.AddTransient<MarketingWindow>();
            sc.AddTransient<FiscalWindow>();
            sc.AddTransient<CaixaWindow>();
            sc.AddTransient<CaixaAberturaWindow>();
            sc.AddTransient<CaixaMovimentoWindow>();
            sc.AddTransient<CaixaFechamentoWindow>();
            sc.AddTransient<PdvWindow>();
            sc.AddTransient<PagamentoVendaWindow>();
            sc.AddTransient<ValorPromptWindow>();
            sc.AddTransient<ProdutoBuscaWindow>();
            sc.AddTransient<SupervisorAutorizacaoWindow>();

            Services = sc.BuildServiceProvider();

            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.Inicializar(db);
            }

            using (var scope = Services.CreateScope())
            {
                var login = scope.ServiceProvider.GetRequiredService<LoginWindow>();
                var ok = login.ShowDialog();
                if (ok != true)
                {
                    Shutdown();
                    return;
                }
            }

            using (var scope = Services.CreateScope())
            {
                var sessao = scope.ServiceProvider.GetRequiredService<SessaoApp>();
                var nfceService = scope.ServiceProvider.GetRequiredService<NfceService>();
                var empresas = await nfceService.ListarEmpresasAsync();
                if (sessao.EmpresaAtiva == null && empresas.Count > 0)
                    sessao.EmpresaAtiva = empresas[0];
            }

            var shell = Services.GetRequiredService<MainWindow>();
            MainWindow = shell;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            shell.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao iniciar:\n\n{ex.Message}\n\nDetalhe:\n{ex.InnerException?.Message}",
                "Erro fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void AjustarJanelaNaTela(object sender, RoutedEventArgs e)
    {
        if (sender is not Window janela) return;
        if (janela.WindowState == WindowState.Maximized) return;

        var area = SystemParameters.WorkArea;
        const double margem = 10;

        janela.MaxWidth = Math.Max(520, area.Width - margem);
        janela.MaxHeight = Math.Max(360, area.Height - margem);

        if (janela.Width > janela.MaxWidth)
            janela.Width = janela.MaxWidth;

        if (janela.Height > janela.MaxHeight)
            janela.Height = janela.MaxHeight;

        var precisaRecentralizar =
            janela.Left < area.Left ||
            janela.Top < area.Top ||
            janela.Left + janela.Width > area.Right ||
            janela.Top + janela.Height > area.Bottom;

        if (!precisaRecentralizar) return;

        janela.Left = area.Left + Math.Max(0, (area.Width - janela.Width) / 2);
        janela.Top = area.Top + Math.Max(0, (area.Height - janela.Height) / 2);
    }
}
