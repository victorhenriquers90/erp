using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Sessao;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class MainViewModel : BaseViewModel
{
    private readonly SessaoApp _sessao;
    private readonly IServiceProvider _sp;

    [ObservableProperty] private BaseViewModel? _paginaAtual;
    [ObservableProperty] private string _nomeUsuario  = "Administrador";
    [ObservableProperty] private string _empresa      = "Loja Exemplo";
    [ObservableProperty] private string _paginaTitulo = "Painel";
    [ObservableProperty] private bool   _menuAberto   = true;
    [ObservableProperty] private string _erro         = string.Empty;

    public MainViewModel(SessaoApp sessao, IServiceProvider sp)
    {
        System.Diagnostics.Trace.WriteLine("[MainViewModel] Construtor iniciado");
        _sessao = sessao;
        _sp     = sp;
        NomeUsuario  = sessao.UsuarioLogado?.Nome ?? "Administrador";
        Empresa      = sessao.EmpresaAtiva?.NomeFantasia ?? "Loja Exemplo";

        System.Diagnostics.Trace.WriteLine($"[MainViewModel] Usuário: {NomeUsuario}, Empresa: {Empresa}");

        // FIX DEADLOCK: Carregar Dashboard SINCRONAMENTE no construtor, SEM await
        try
        {
            System.Diagnostics.Trace.WriteLine("[MainViewModel] Carregando Dashboard NO CONSTRUTOR (síncrono)...");
            AbrirDashboard();
            System.Diagnostics.Trace.WriteLine("[MainViewModel] ✅ Dashboard carregado com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[MainViewModel] ❌ ERRO ao abrir Dashboard: {ex}");
            Erro = $"Erro ao carregar Dashboard: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task AbrirClientesAsync()
    {
        System.Diagnostics.Trace.WriteLine("[MainViewModel] AbrirClientesAsync iniciado");
        await AbrirAsync<ClientesViewModel>("Clientes", vm => vm.CarregarCommand.ExecuteAsync(null));
        System.Diagnostics.Trace.WriteLine("[MainViewModel] AbrirClientesAsync finalizado");
    }

    [RelayCommand]
    private async Task AbrirProdutosAsync()
    {
        System.Diagnostics.Trace.WriteLine("[MainViewModel] AbrirProdutosAsync iniciado");
        await AbrirAsync<ProdutosViewModel>("Produtos", vm => vm.CarregarCommand.ExecuteAsync(null));
        System.Diagnostics.Trace.WriteLine("[MainViewModel] AbrirProdutosAsync finalizado");
    }

    [RelayCommand]
    private async Task AbrirFornecedoresAsync()
    {
        System.Diagnostics.Trace.WriteLine("[MainViewModel] AbrirFornecedoresAsync iniciado");
        await AbrirAsync<FornecedoresViewModel>("Fornecedores", vm => vm.CarregarCommand.ExecuteAsync(null));
        System.Diagnostics.Trace.WriteLine("[MainViewModel] AbrirFornecedoresAsync finalizado");
    }

    [RelayCommand]
    private async Task AbrirEstoqueAsync()
    {
        await AbrirAsync<EstoqueViewModel>("Estoque", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirFinanceiroAsync()
    {
        await AbrirAsync<FinanceiroViewModel>("Financeiro", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirVendasAsync()
    {
        await AbrirAsync<VendasViewModel>("Vendas", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirCaixaAsync()
    {
        await AbrirAsync<CaixaViewModel>("Caixa", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirUsuariosAsync()
    {
        await AbrirAsync<UsuariosViewModel>("Usuários", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirFiliaisAsync()
    {
        await AbrirAsync<FiliaisViewModel>("Filiais", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirRelatoriosAsync()
    {
        await AbrirAsync<RelatoriosViewModel>("Relatórios", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirAuditoriaAsync()
    {
        await AbrirAsync<AuditoriaViewModel>("Auditoria", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private void AbrirFiscal()
    {
        PaginaTitulo = "Fiscal";
        PaginaAtual = _sp.GetRequiredService<FiscalViewModel>();
    }

    [RelayCommand]
    private void AbrirOperacoes()
    {
        PaginaTitulo = "Operações";
        PaginaAtual = _sp.GetRequiredService<OperacoesViewModel>();
    }

    [RelayCommand]
    private void AbrirAdministracaoSistema()
    {
        PaginaTitulo = "Administração";
        PaginaAtual = _sp.GetRequiredService<AdministracaoSistemaViewModel>();
    }

    [RelayCommand]
    private void ToggleMenu() => MenuAberto = !MenuAberto;

    private void AbrirDashboard()
    {
        System.Diagnostics.Trace.WriteLine("[MainViewModel.AbrirDashboard] Obtendo DashboardViewModel...");
        PaginaTitulo = "Painel";
        PaginaAtual = _sp.GetRequiredService<DashboardViewModel>();
        System.Diagnostics.Trace.WriteLine("[MainViewModel.AbrirDashboard] ✅ DashboardViewModel obtido e definido");
    }

    private async Task AbrirAsync<TViewModel>(string titulo, Func<TViewModel, Task> carregador)
        where TViewModel : BaseViewModel
    {
        try
        {
            System.Diagnostics.Trace.WriteLine($"[MainViewModel.AbrirAsync] Abrindo {titulo}...");
            SetBusy(true, $"Carregando {titulo}...");

            System.Diagnostics.Trace.WriteLine($"[MainViewModel.AbrirAsync] Obtendo {typeof(TViewModel).Name} do DI...");
            PaginaTitulo = titulo;
            var vm = _sp.GetRequiredService<TViewModel>();
            System.Diagnostics.Trace.WriteLine($"[MainViewModel.AbrirAsync] ViewModel obtido, definindo PaginaAtual...");

            PaginaAtual = vm;
            System.Diagnostics.Trace.WriteLine($"[MainViewModel.AbrirAsync] Executando carregador...");

            await carregador(vm);
            System.Diagnostics.Trace.WriteLine($"[MainViewModel.AbrirAsync] ✅ {titulo} carregado com sucesso");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"[MainViewModel.AbrirAsync] ❌ ERRO ao carregar {titulo}: {ex}");
            Erro = $"Erro ao carregar {titulo}: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }
}
