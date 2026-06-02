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
using ProjetoVarejo.Infrastructure.Reporting;
using ProjetoVarejo.Infrastructure.WhatsApp;
using ProjetoVarejo.Infrastructure.Nfce;

namespace ProjetoVarejo.Desktop.Wpf;

public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        ThemeManager.AplicarTemaSalvo();
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(AjustarJanelaNaTela));

        // Captura global de erros (confiabilidade / suporte em campo)
        DispatcherUnhandledException += (_, args) =>
        {
            AppLog.Erro("UI", args.Exception);
            MessageBox.Show(
                $"Ocorreu um erro inesperado:\n\n{args.Exception.Message}\n\nO erro foi registrado em:\n{AppLog.CaminhoArquivoHoje}",
                "Projeto ERP", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            AppLog.Erro("AppDomain", args.ExceptionObject as Exception ?? new Exception("Erro fatal não tratado"));
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLog.Erro("Task", args.Exception);
            args.SetObserved();
        };

        try
        {
            // ── Gate de licenciamento (anti-pirataria, offline) ──
            var licenca = Licensing.LicenseService.Validar();
            if (!licenca.Valida)
            {
                var ativacao = new ActivationWindow();
                if (ativacao.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }
            }

            // ── Wizard de primeiro uso (modo Servidor / Cliente) ──
            var modo = ModoSistema.Carregar();
            if (!modo.EstaConfigurado)
            {
                var wizard = new SetupWizardWindow();
                if (wizard.ShowDialog() != true)
                {
                    Shutdown();
                    return;
                }
                modo = ModoSistema.Carregar(); // recarrega após o wizard salvar
            }

            // Expor o modo globalmente (diagnóstico, UI informativa)
            Current.Resources["ModoSistema"] = modo;
            var apiClient = modo.EhCliente ? new ApiClient(modo.UrlApi!) : null;
            if (apiClient != null)
                Current.Resources["ApiClient"] = apiClient;

            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            // No modo Servidor usa a connection string configurada pelo wizard;
            // no modo Cliente usa a da API (o banco não é acessado diretamente).
            var connStr = modo.EhServidor && !string.IsNullOrWhiteSpace(modo.ConnectionString)
                ? modo.ConnectionString
                : config.GetConnectionString("Default")!;

            var sc = new ServiceCollection();
            sc.AddSingleton<IConfiguration>(config);
            sc.AddSingleton(modo);
            if (apiClient != null) sc.AddSingleton(apiClient);
            sc.AddSingleton<SessaoApp>();
            sc.AddScoped<AuditSaveChangesInterceptor>();
            sc.AddDbContext<AppDbContext>((sp, opt) =>
                opt.UseSqlServer(connStr)
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
            sc.AddScoped<FilialPainelService>();
            sc.AddScoped<RelatorioExporter>();
            sc.AddSingleton(new WhatsAppConfig());
            sc.AddScoped<WhatsAppService>();
            sc.AddScoped<NotificacaoService>();
            sc.AddSingleton<AlertaMonitorService>();
            sc.AddSingleton<CupomPrinterService>();
            sc.AddScoped<BackupService>();
            sc.AddScoped<DadosDemoService>();
            sc.AddScoped<NfeImporterService>();
            sc.AddScoped<PermissaoService>();
            sc.AddScoped<AuditLogService>();
            sc.AddSingleton<NfceXmlGenerator>();
            sc.AddSingleton<NfceAssinador>();
            sc.AddSingleton<SefazSpClient>();
            sc.AddSingleton<NfceCancelamentoBuilder>();
            sc.AddSingleton<NfceInutilizacaoBuilder>();
            sc.AddScoped<NfceService>();
            sc.AddSingleton<ProjetoVarejo.Infrastructure.Nfe.NfeXmlGenerator>();
            sc.AddScoped<NfeService>();

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
            sc.AddTransient<ConfiguracoesWindow>();
            sc.AddTransient<FaturamentoWindow>();
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

            IniciarBackupDiarioEmBackground();
            IniciarTimerAlertasWhatsApp();
        }
        catch (Exception ex)
        {
            AppLog.Erro("Startup", ex);
            MessageBox.Show(
                $"Erro ao iniciar:\n\n{ex.Message}\n\nDetalhe:\n{ex.InnerException?.Message}\n\nLog: {AppLog.CaminhoArquivoHoje}",
                "Erro fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    /// <summary>
    /// Backup automático diário (best-effort, em segundo plano). Só executa se ainda não
    /// houver um .bak criado hoje. Falhas são apenas registradas no log, sem incomodar o usuário.
    /// </summary>
    private void IniciarBackupDiarioEmBackground()
    {
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var pasta = System.IO.Path.Combine(AppContext.BaseDirectory, "Backups");
                if (System.IO.Directory.Exists(pasta))
                {
                    var temBackupHoje = new System.IO.DirectoryInfo(pasta)
                        .GetFiles("*.bak")
                        .Any(f => f.CreationTime.Date == DateTime.Today);
                    if (temBackupHoje) return;
                }

                await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(8)); // deixa a UI subir
                using var scope = Services.CreateScope();
                var backup = scope.ServiceProvider.GetRequiredService<BackupService>();
                var r = await backup.ExecutarAsync();
                if (!r.Sucesso)
                    AppLog.Erro("BackupAuto", new Exception(r.Erro ?? "Falha no backup automático."));
            }
            catch (Exception ex)
            {
                AppLog.Erro("BackupAuto", ex);
            }
        });
    }

    /// <summary>
    /// Inicia o timer de alertas WhatsApp com base na configuração salva em disco.
    /// Só ativa o timer se AlertaConfig.Ativo == true; o intervalo é recarregado do arquivo.
    /// </summary>
    private void IniciarTimerAlertasWhatsApp()
    {
        var cfg = ProjetoVarejo.Application.Services.AlertaConfig.Carregar();
        if (!cfg.Ativo) return;

        var monitor = Services.GetRequiredService<ProjetoVarejo.Application.Services.AlertaMonitorService>();
        monitor.AlertasAtivos = cfg.Ativo;
        monitor.TelefoneGerente = cfg.TelefoneGerente;
        monitor.HorarioCaixaDeveAbrirAte = TimeSpan.FromHours(cfg.HoraCaixaDeveAbrir);

        var intervalo = TimeSpan.FromMinutes(Math.Max(1, cfg.IntervaloVerificacaoMinutos));
        var timer = new System.Timers.Timer(intervalo.TotalMilliseconds) { AutoReset = true };
        timer.Elapsed += async (_, _) =>
        {
            try
            {
                await monitor.VerificarAsync();
            }
            catch (Exception ex)
            {
                AppLog.Erro("AlertaTimer", ex);
            }
        };
        timer.Start();
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
