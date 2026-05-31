using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Sessao;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAutenticacaoService _auth;
    private readonly SessaoApp _sessao;

    [ObservableProperty] private string _login  = "admin";
    [ObservableProperty] private string _erro   = string.Empty;
    [ObservableProperty] private bool   _loginOk;

    public LoginViewModel(IAutenticacaoService auth, SessaoApp sessao)
    {
        _auth   = auth;
        _sessao = sessao;
    }

    [RelayCommand(CanExecute = nameof(PodeEntrar))]
    private async Task EntrarAsync(string senha)
    {
        Erro = string.Empty;
        SetBusy(true, "Autenticando...");
        try
        {
            var resultado = await _auth.LoginAsync(Login, senha);
            if (!resultado.Sucesso)
            {
                Erro = resultado.Erro ?? "Usuário ou senha incorretos.";
                return;
            }
            _sessao.DefinirUsuario(resultado.Valor!);
            LoginOk = true;
        }
        catch (Exception ex)
        {
            Erro = $"Erro de conexão: {ex.Message}";
        }
        finally { SetBusy(false); }
    }

    private bool PodeEntrar(string _) => !IsBusy && !string.IsNullOrWhiteSpace(Login);
}
