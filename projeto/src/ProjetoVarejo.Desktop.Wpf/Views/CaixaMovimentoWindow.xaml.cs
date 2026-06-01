using System.Globalization;
using System.Windows;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class CaixaMovimentoWindow : Window
{
    private readonly CultureInfo _ptBr = new("pt-BR");
    private decimal _valor;
    private string _motivo = string.Empty;

    public CaixaMovimentoWindow()
    {
        InitializeComponent();
    }

    public bool Abrir(Window owner, string titulo, string subtitulo, out decimal valor, out string motivo)
    {
        Owner = owner;
        LblTitulo.Text = titulo;
        LblSub.Text = subtitulo;
        TxtValor.Text = "0,00";
        TxtMotivo.Text = string.Empty;
        _valor = 0;
        _motivo = string.Empty;

        var ok = ShowDialog() == true;
        valor = _valor;
        motivo = _motivo;
        return ok;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtValor.Text, NumberStyles.Any, _ptBr, out var valor) || valor <= 0)
        {
            MessageBox.Show("Valor inválido.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var motivo = TxtMotivo.Text.Trim();
        if (string.IsNullOrWhiteSpace(motivo))
        {
            MessageBox.Show("Informe o motivo.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _valor = valor;
        _motivo = motivo;
        DialogResult = true;
        Close();
    }
}
