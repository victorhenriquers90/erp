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

    public MainViewModel(SessaoApp sessao, IServiceProvider sp)
    {
        _sessao = sessao;
        _sp     = sp;
        NomeUsuario  = sessao.UsuarioLogado?.Nome ?? "Administrador";
        Empresa      = sessao.EmpresaAtiva?.NomeFantasia ?? "Loja Exemplo";

        // Abre Dashboard por padrão
        AbrirDashboard();
    }

    [RelayCommand]
    private void AbrirDashboard()
    {
        PaginaTitulo = "Painel";
        PaginaAtual  = _sp.GetRequiredService<DashboardViewModel>();
    }

    [RelayCommand]
    private async Task AbrirClientesAsync()
    {
        await AbrirAsync<ClientesViewModel>("Clientes", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirProdutosAsync()
    {
        await AbrirAsync<ProdutosViewModel>("Produtos", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirFornecedoresAsync()
    {
        await AbrirAsync<FornecedoresViewModel>("Fornecedores", vm => vm.CarregarCommand.ExecuteAsync(null));
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
        await AbrirAsync<VendasViewModel>("PDV / Vendas", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirCaixaAsync()
    {
        await AbrirAsync<CaixaViewModel>("Caixa", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirUsuariosAsync()
    {
        await AbrirAsync<UsuariosViewModel>("Usuarios", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirFiliaisAsync()
    {
        await AbrirAsync<FiliaisViewModel>("Filiais", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirRelatoriosAsync()
    {
        await AbrirAsync<RelatoriosViewModel>("Relatorios", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private async Task AbrirAuditoriaAsync()
    {
        await AbrirAsync<AuditoriaViewModel>("Auditoria", vm => vm.CarregarCommand.ExecuteAsync(null));
    }

    [RelayCommand]
    private void AbrirFiscal()
    {
        PaginaTitulo = "Fiscal e pagamentos";
        PaginaAtual = _sp.GetRequiredService<FiscalViewModel>();
    }

    [RelayCommand]
    private void AbrirOperacoes()
    {
        PaginaTitulo = "Operacoes de loja";
        PaginaAtual = _sp.GetRequiredService<OperacoesViewModel>();
    }

    [RelayCommand]
    private void AbrirAdministracaoSistema()
    {
        PaginaTitulo = "Administracao do sistema";
        PaginaAtual = _sp.GetRequiredService<AdministracaoSistemaViewModel>();
    }

    [RelayCommand]
    private void ToggleMenu() => MenuAberto = !MenuAberto;

    private async Task AbrirAsync<TViewModel>(string titulo, Func<TViewModel, Task> carregar)
        where TViewModel : BaseViewModel
    {
        PaginaTitulo = titulo;
        var vm = _sp.GetRequiredService<TViewModel>();
        PaginaAtual = vm;
        await carregar(vm);
    }
}
