using System.Windows;
using System.Windows.Input;
using ProjetoVarejo.Application.Services;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class LoginWindow : Window
{
    private readonly AutenticacaoService _autenticacaoService;

    public LoginWindow(AutenticacaoService autenticacaoService)
    {
        _autenticacaoService = autenticacaoService;
        InitializeComponent();
        Loaded += (_, _) =>
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                TxtLogin.Focus();
                Keyboard.Focus(TxtLogin);
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        };
        KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Return)
                Entrar_Click(this, new RoutedEventArgs());
        };
    }

    private void LoginField_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        TxtLogin.Focus();
        Keyboard.Focus(TxtLogin);
    }

    private void SenhaField_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        TxtSenha.Focus();
        Keyboard.Focus(TxtSenha);
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
            var senha = TxtSenhaPlain.Visibility == Visibility.Visible ? TxtSenhaPlain.Text : TxtSenha.Password;
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

    private void BtnToggleSenha_Click(object sender, RoutedEventArgs e)
    {
        if (TxtSenhaPlain.Visibility == Visibility.Visible)
        {
            TxtSenha.Password = TxtSenhaPlain.Text;
            TxtSenhaPlain.Visibility = Visibility.Collapsed;
            TxtSenha.Visibility = Visibility.Visible;
            TxtSenha.Focus();
        }
        else
        {
            TxtSenhaPlain.Text = TxtSenha.Password;
            TxtSenhaPlain.Visibility = Visibility.Visible;
            TxtSenha.Visibility = Visibility.Collapsed;
            TxtSenhaPlain.Focus();
        }
    }
}
