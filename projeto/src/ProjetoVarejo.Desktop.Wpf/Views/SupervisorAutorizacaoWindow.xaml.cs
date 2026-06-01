using System.Windows;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class SupervisorAutorizacaoWindow : Window
{
    private string _login = string.Empty;
    private string _senha = string.Empty;

    public SupervisorAutorizacaoWindow()
    {
        InitializeComponent();
    }

    public bool Abrir(Window owner, string titulo, string mensagem, out string login, out string senha)
    {
        Owner = owner;
        LblTitulo.Text = titulo;
        LblMensagem.Text = mensagem;
        TxtLogin.Text = string.Empty;
        TxtSenha.Password = string.Empty;
        _login = string.Empty;
        _senha = string.Empty;

        var ok = ShowDialog() == true;
        login = _login;
        senha = _senha;
        return ok;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Autorizar_Click(object sender, RoutedEventArgs e)
    {
        var login = TxtLogin.Text.Trim();
        var senha = TxtSenha.Password;
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(senha))
        {
            MessageBox.Show("Informe login e senha do supervisor.", "Autorização", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _login = login;
        _senha = senha;
        DialogResult = true;
        Close();
    }
}
