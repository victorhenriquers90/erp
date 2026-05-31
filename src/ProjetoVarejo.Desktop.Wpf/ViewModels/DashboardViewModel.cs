using CommunityToolkit.Mvvm.ComponentModel;
using ProjetoVarejo.Application.Sessao;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class DashboardViewModel : BaseViewModel
{
    private readonly SessaoApp _sessao;

    [ObservableProperty] private string _nomeUsuario = "Administrador";
    [ObservableProperty] private string _empresa     = "Loja Exemplo";
    [ObservableProperty] private decimal _vendasHoje;
    [ObservableProperty] private int    _transacoesHoje;
    [ObservableProperty] private int    _alertasEstoque;

    public DashboardViewModel(SessaoApp sessao)
    {
        _sessao = sessao;
        NomeUsuario = sessao.UsuarioLogado?.Nome ?? "Administrador";
        Empresa     = sessao.EmpresaAtiva?.NomeFantasia ?? sessao.EmpresaAtiva?.RazaoSocial ?? "Loja Exemplo";
    }
}
