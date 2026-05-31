using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Desktop.Wpf.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel vm)
    {
        _vm = vm;
        InitializeComponent();
    }

    private async void BtnEntrar_Click(object sender, RoutedEventArgs e)
        => await EntrarAsync();

    private async void TxtSenha_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await EntrarAsync();
    }

    private async Task EntrarAsync()
    {
        TxtErro.Visibility      = Visibility.Collapsed;
        BtnEntrar.IsEnabled     = false;
        PbLoading.Visibility    = Visibility.Visible;

        var senha = TxtSenha.Password;
        await _vm.EntrarCommand.ExecuteAsync(senha);

        if (_vm.LoginOk)
        {
            var main = App.Services.GetRequiredService<MainWindow>();
            main.Show();
            Close();
        }
        else
        {
            TxtErro.Text       = _vm.Erro;
            TxtErro.Visibility = Visibility.Visible;
        }

        BtnEntrar.IsEnabled  = true;
        PbLoading.Visibility = Visibility.Collapsed;
    }
}
