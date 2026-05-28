using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Auditing;
using ProjetoVarejo.Application.Configuracao;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Logging;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Application.Validators;
using ProjetoVarejo.Desktop.Forms;
using ProjetoVarejo.Infrastructure.Backup;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Infrastructure.Nfce;
using ProjetoVarejo.Infrastructure.Repositories;
using ProjetoVarejo.Infrastructure.Services;
using Serilog;
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
            Log.Error(e.Exception, "ThreadException não tratada");
            MessageBox.Show("Erro: " + e.Exception.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Log.Fatal((Exception)e.ExceptionObject, "UnhandledException não tratada");
        };

        try
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                .Build();

            // Configurar Serilog PRIMEIRO antes de qualquer outra coisa
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var connectionString = config.GetConnectionString("Default") ?? string.Empty;
            LoggingConfiguration.ConfigureSerilog(environment, connectionString);

            Log.Information(LogTemplates.AplicacaoIniciada, "1.0.0", environment);

            var sc = new ServiceCollection();
            sc.AddSingleton<IConfiguration>(config);
            sc.AddSingleton<SessaoApp>();
            sc.AddScoped<AuditSaveChangesInterceptor>();
            sc.AddDbContext<AppDbContext>((sp, opt) =>
                opt.UseSqlServer(config.GetConnectionString("Default"),
                    sqlOpt => sqlOpt.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(3),
                        errorNumbersToAdd: null))
                   .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

            // 🏗️ FASE 2: Dependency Inversion - Repository Pattern + Unit of Work
            sc.AddScoped<IUnitOfWork, UnitOfWork>();

            // PHASE 5: FluentValidation - Centralized Validation
            sc.AddScoped<IValidator<Usuario>, UsuarioValidator>();
            sc.AddScoped<IValidator<Produto>, ProdutoValidator>();
            sc.AddScoped<IValidator<Venda>, VendaValidator>();
            sc.AddScoped<IValidator<ItemVenda>, ItemVendaValidator>();
            sc.AddScoped<IValidator<PagamentoVenda>, PagamentoVendaValidator>();
            sc.AddScoped<IValidator<CaixaSessao>, CaixaSessionValidator>();
            sc.AddScoped<IValidator<NotaFiscal>, NotaFiscalValidator>();
            sc.AddScoped<IValidator<Categoria>, CategoriaValidator>();

            // PHASE 3: Service Interfaces for Abstraction
            sc.AddScoped<IAutenticacaoService, AutenticacaoService>();
            sc.AddScoped<IProdutoService, ProdutoService>();
            sc.AddScoped<IEstoqueService, EstoqueService>();
            sc.AddScoped<IVendaService, VendaService>();
            sc.AddScoped<IFinanceiroService, FinanceiroService>();
            sc.AddScoped<ICaixaService, CaixaService>();
            sc.AddScoped<IRelatorioService, RelatorioService>();
            sc.AddScoped<INfceService, NfceService>(); // PHASE 2.5-3: NfceService (Infrastructure.Services)
            sc.AddScoped<ICupomPrinterService, CupomPrinterService>(); // PHASE 2.5: CupomPrinterService (Infrastructure.Services)

            // Services without interfaces (can be added in future phases)
            sc.AddScoped<ClienteService>();
            sc.AddScoped<FornecedorService>();
            sc.AddScoped<CategoriaService>();
            sc.AddScoped<ChecklistProducaoService>();
            sc.AddScoped<UsuarioService>();
            sc.AddScoped<ProducaoGuardService>();
            sc.AddSingleton<ImplantacaoService>();
            sc.AddScoped<BackupService>();
            sc.AddScoped<NfeImporterService>();
            sc.AddScoped<PermissaoService>();
            sc.AddScoped<AuditLogService>();

            // PHASE 2.5-3: NfceService Infrastructure Components (NFC-e Generation & Signing)
            sc.AddSingleton<NfceXmlGenerator>();
            sc.AddSingleton<NfceAssinador>();
            sc.AddSingleton<SefazSpClient>();
            sc.AddSingleton<NfceCancelamentoBuilder>();
            sc.AddSingleton<NfceInutilizacaoBuilder>();
            // NfceService itself registered above with INfceService interface

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
            sc.AddTransient<FrmFechamentoDia>();

            Services = sc.BuildServiceProvider();

            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DbInitializer.Inicializar(db);
            }

            // O instalador cria firstrun.flag na pasta do app.
            // Enquanto esse arquivo existir, o wizard de configuração é exibido
            // independente do estado do banco — cobre fresh installs e reinstalações.
            var flagFile = Path.Combine(AppContext.BaseDirectory, "firstrun.flag");
            var primeiraVez = File.Exists(flagFile);

            // Verificar se precisa fazer setup inicial
            using (var setupScope = Services.CreateScope())
            {
                var validador = setupScope.ServiceProvider.GetRequiredService<ValidadorSetupInicial>();
                var naoConfigurado = validador.PrecisaDeSetupInicial().GetAwaiter().GetResult();

                if (primeiraVez || naoConfigurado)
                {
                    var frmSetup = setupScope.ServiceProvider.GetRequiredService<FrmConfiguracao>();
                    frmSetup.ShowDialog();

                    // Apagar a flag independente do resultado — não queremos mostrar
                    // de novo na próxima abertura
                    try { if (File.Exists(flagFile)) File.Delete(flagFile); } catch { }

                    if (frmSetup.DialogResult != DialogResult.OK)
                    {
                        if (naoConfigurado)
                        {
                            // Sem nenhuma configuração válida — não tem como continuar
                            MessageBox.Show(
                                "O sistema não foi configurado. A aplicação será encerrada.",
                                "Setup Cancelado",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            return;
                        }
                        // Reinstalação: usuário cancelou mas já havia config no banco — continua normalmente
                    }
                }
            }

            // Task de backup rastreada
            var backupTask = Task.Run(async () =>
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
                catch (System.OperationCanceledException) { }
                catch { }
            });

            using var loginScope = Services.CreateScope();
            var login = loginScope.ServiceProvider.GetRequiredService<FrmLogin>();
            WinFormsApp.Run(login);

            // Aguardar task de backup ao fechar (máximo 5 segundos)
            try
            {
                if (!backupTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    Log.Warning("Task de backup não completou no tempo esperado");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Erro ao aguardar task de backup");
            }

            var sessao = Services.GetRequiredService<SessaoApp>();
            if (sessao.Autenticado)
            {
                // Seleção de empresa ativa (se houver mais de uma cadastrada)
                using (var seScope = Services.CreateScope())
                {
                    var nfceSvc = seScope.ServiceProvider.GetRequiredService<INfceService>();
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
            Log.Fatal(ex, "Erro fatal ao iniciar aplicação");
            MessageBox.Show(
                $"Erro ao iniciar:\n\n{ex.Message}\n\nDetalhe:\n{ex.InnerException?.Message}",
                "Erro fatal", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Log.Information(LogTemplates.AplicacaoFinalizada);
            LoggingConfiguration.FlushAndClose();
        }
    }
}
