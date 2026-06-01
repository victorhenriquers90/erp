using System.Windows;
using ProjetoVarejo.Application.Services;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class LoginWindow : Window
{
    private readonly AutenticacaoService _autenticacaoService;

    public LoginWindow(AutenticacaoService autenticacaoService)
    {
        _autenticacaoService = autenticacaoService;
        InitializeComponent();
    }

    private void Sair_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void Entrar_Click(object sender, RoutedEventArgs e)
    {
        ErroBox.Visibility = Visibility.Collapsed;
        BtnEntrar.IsEnabled = false;
        BtnEntrar.Content = "Verificando...";
        try
        {
            var login = TxtLogin.Text.Trim();
            var senha = TxtSenha.Password;
            var result = await _autenticacaoService.LoginAsync(login, senha);
            if (!result.Sucesso)
            {
                LblErro.Text = result.Erro ?? "Falha na autenticação.";
                ErroBox.Visibility = Visibility.Visible;
                return;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            LblErro.Text = $"Erro ao autenticar: {ex.Message}";
            ErroBox.Visibility = Visibility.Visible;
        }
        finally
        {
            BtnEntrar.IsEnabled = true;
            BtnEntrar.Content = "Entrar";
        }
    }
}
