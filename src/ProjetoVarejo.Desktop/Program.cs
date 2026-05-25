using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Auditing;
using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Desktop.Forms;
using ProjetoVarejo.Infrastructure.Backup;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Infrastructure.Nfce;
using WinFormsApp = System.Windows.Forms.Application;

namespace ProjetoVarejo.Desktop;

static class Program
{
    public static IServiceProvider Services { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        WinFormsApp.ThreadException += (s, e) =>
        {
            File.WriteAllText("startup_error.log", $"ThreadException:\n{e.Exception}\n");
            MessageBox.Show("Erro: " + e.Exception.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            File.WriteAllText("startup_error.log", $"UnhandledException:\n{e.ExceptionObject}\n");
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
            sc.AddScoped<ChecklistProducaoService>();
            sc.AddScoped<UsuarioService>();
            sc.AddScoped<ProducaoGuardService>();
            sc.AddSingleton<ImplantacaoService>();
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
            sc.AddScoped<ConfiguracaoNegocioService>();
            sc.AddScoped<ValidadorSetupInicial>();

            sc.AddTransient<FrmLogin>();
            sc.AddTransient<FrmConfiguracao>();
            sc.AddTransient<FrmMain>();
            sc.AddTransient<FrmPdv>();
            sc.AddTransient<FrmProdutos>();
            sc.AddTransient<FrmClientes>();
            sc.AddTransient<FrmFornecedores>();
            sc.AddTransient<FrmEstoque>();
            sc.AddTransient<FrmFinanceiro>();
            sc.AddTransient<FrmConfigEmpresa>();
            sc.AddTransient<FrmNotasFiscais>();
            sc.AddTransient<FrmCaixa>();
            sc.AddTransient<FrmRelatorios>();
            sc.AddTransient<FrmBackup>();
            sc.AddTransient<FrmImportarNfe>();
            sc.AddTransient<FrmSelecionarEmpresa>();
            sc.AddTransient<FrmAuditoria>();
            sc.AddTransient<FrmChecklistProducao>();
            sc.AddTransient<FrmUsuarios>();
            sc.AddTransient<FrmImplantacao>();
            sc.AddTransient<FrmGerenciadorModulos>();

            Services = sc.BuildServiceProvider();

            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.Inicializar(db);
            }

            // Verificar se precisa fazer setup inicial
            using (var setupScope = Services.CreateScope())
            {
                var validador = setupScope.ServiceProvider.GetRequiredService<ValidadorSetupInicial>();
                var precisaSetup = validador.PrecisaDeSetupInicial().GetAwaiter().GetResult();

                if (precisaSetup)
                {
                    var frmSetup = setupScope.ServiceProvider.GetRequiredService<FrmConfiguracao>();
                    frmSetup.ShowDialog();

                    if (frmSetup.DialogResult != DialogResult.OK)
                    {
                        // Usuário cancelou o setup
                        MessageBox.Show(
                            "O sistema não foi configurado. A aplicação será encerrada.",
                            "Setup Cancelado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var cfgFile = Path.Combine(AppContext.BaseDirectory, "backup.cfg");
                    var ultimoFile = Path.Combine(AppContext.BaseDirectory, "backup.last");
                    if (!File.Exists(cfgFile)) return;
                    var linhas = File.ReadAllLines(cfgFile);
                    if (linhas.Length < 2 || linhas[1] != "1") return;
                    var pasta = linhas[0];

                    if (File.Exists(ultimoFile))
                    {
                        var ult = File.GetLastWriteTime(ultimoFile);
                        if (ult.Date == DateTime.Today) return;
                    }
                    using var s = Services.CreateScope();
                    var bk = s.ServiceProvider.GetRequiredService<BackupService>();
                    var r = await bk.ExecutarAsync(pasta);
                    if (r.Sucesso) File.WriteAllText(ultimoFile, DateTime.Now.ToString("o"));
                }
                catch { }
            });

            using var loginScope = Services.CreateScope();
            var login = loginScope.ServiceProvider.GetRequiredService<FrmLogin>();
            WinFormsApp.Run(login);

            var sessao = Services.GetRequiredService<SessaoApp>();
            if (sessao.Autenticado)
            {
                using (var seScope = Services.CreateScope())
                {
                    var nfceSvc = seScope.ServiceProvider.GetRequiredService<NfceService>();
                    var empresas = nfceSvc.ListarEmpresasAsync().GetAwaiter().GetResult();
                    if (empresas.Count > 1)
                    {
                        var fSel = seScope.ServiceProvider.GetRequiredService<FrmSelecionarEmpresa>();
                        WinFormsApp.Run(fSel);
                    }
                    else if (empresas.Count == 1)
                    {
                        sessao.EmpresaAtiva = empresas[0];
                    }
                }

                using var mainScope = Services.CreateScope();
                var main = mainScope.ServiceProvider.GetRequiredService<FrmMain>();
                WinFormsApp.Run(main);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Erro ao iniciar:\n\n{ex.Message}\n\nDetalhe:\n{ex.InnerException?.Message}",
                "Erro fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
