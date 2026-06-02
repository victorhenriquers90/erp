using System.Windows;
using ProjetoVarejo.Desktop.Wpf.Licensing;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ActivationWindow : Window
{
    public ActivationWindow()
    {
        InitializeComponent();
        TxtFingerprint.Text = LicenseService.ObterFingerprint();
    }

    private void Copiar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(TxtFingerprint.Text);
            MessageBox.Show("Código copiado.", "Ativação", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch { /* clipboard pode falhar em alguns ambientes */ }
    }

    private void Ativar_Click(object sender, RoutedEventArgs e)
    {
        ErroBox.Visibility = Visibility.Collapsed;
        var chave = TxtChave.Text.Trim();
        if (string.IsNullOrWhiteSpace(chave))
        {
            MostrarErro("Cole a chave de licença recebida.");
            return;
        }

        var info = LicenseService.Ativar(chave);
        if (info.Valida)
        {
            var validade = info.Expira.HasValue ? $"\nVálida até: {info.Expira:dd/MM/yyyy}" : "\nLicença perpétua.";
            MessageBox.Show($"Licença ativada com sucesso!\nCliente: {info.Cliente}{validade}",
                "Ativação", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        else
        {
            MostrarErro(info.Motivo);
        }
    }

    private void MostrarErro(string msg)
    {
        LblErro.Text = msg;
        ErroBox.Visibility = Visibility.Visible;
    }

    private void Sair_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
