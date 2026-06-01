using System.Globalization;
using System.Windows;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class CaixaAberturaWindow : Window
{
    private readonly CultureInfo _ptBr = new("pt-BR");
    private decimal _valor;

    public CaixaAberturaWindow()
    {
        InitializeComponent();
    }

    public bool Abrir(Window owner, out decimal valorAbertura)
    {
        Owner = owner;
        TxtValor.Text = "0,00";
        _valor = 0;
        var ok = ShowDialog() == true;
        valorAbertura = _valor;
        return ok;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtValor.Text, NumberStyles.Any, _ptBr, out var valor) || valor < 0)
        {
            MessageBox.Show("Valor inválido.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _valor = valor;
        DialogResult = true;
        Close();
    }
}
