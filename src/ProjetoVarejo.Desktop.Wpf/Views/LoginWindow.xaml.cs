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
        DataContext = _vm;
    }

    private async void BtnEntrar_Click(object sender, RoutedEventArgs e)
        => await EntrarAsync();

    private async void TxtSenha_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await EntrarAsync();
    }

    private async Task EntrarAsync()
    {
        PnlErro.Visibility   = Visibility.Collapsed;
        BtnEntrar.IsEnabled  = false;
        PbLoading.Visibility = Visibility.Visible;

        _vm.Login = TxtLogin.Text.Trim();
        var senha = TxtSenha.Password;
        await _vm.EntrarCommand.ExecuteAsync(senha);

        if (_vm.LoginOk)
        {
            System.Diagnostics.Trace.WriteLine("[LoginWindow] Login OK, obtendo MainWindow do DI...");
            try
            {
                var main = App.Services.GetRequiredService<MainWindow>();
                System.Diagnostics.Trace.WriteLine("[LoginWindow] MainWindow obtida, chamando Show()...");
                System.Windows.Application.Current.MainWindow = main;
                main.Show();
                System.Diagnostics.Trace.WriteLine("[LoginWindow] MainWindow mostrada, fechando LoginWindow...");
                Close();
                System.Diagnostics.Trace.WriteLine("[LoginWindow] LoginWindow fechada");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"[LoginWindow] ERRO ao abrir MainWindow: {ex}");
                TxtErro.Text       = $"Erro ao abrir painel: {ex.Message}";
                PnlErro.Visibility = Visibility.Visible;
            }
        }
        else
        {
            TxtErro.Text       = _vm.Erro;
            PnlErro.Visibility = Visibility.Visible;
        }

        BtnEntrar.IsEnabled  = true;
        PbLoading.Visibility = Visibility.Collapsed;
    }
}
