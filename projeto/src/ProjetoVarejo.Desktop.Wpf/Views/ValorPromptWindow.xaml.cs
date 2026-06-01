using System.Globalization;
using System.Windows;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ValorPromptWindow : Window
{
    private readonly CultureInfo _ptBr = new("pt-BR");
    private decimal _valor;

    public ValorPromptWindow()
    {
        InitializeComponent();
    }

    public bool Abrir(Window owner, string titulo, string campo, decimal valorInicial, out decimal valor)
    {
        Owner = owner;
        LblTitulo.Text = titulo;
        LblCampo.Text = campo;
        TxtValor.Text = valorInicial.ToString("N2", _ptBr);
        _valor = valorInicial;
        var ok = ShowDialog() == true;
        valor = _valor;
        return ok;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtValor.Text, NumberStyles.Any, _ptBr, out var valor))
        {
            MessageBox.Show("Valor inválido.", "PDV", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _valor = valor;
        DialogResult = true;
        Close();
    }
}
