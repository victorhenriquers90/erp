using System.Globalization;
using System.Windows;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class QuitacaoContaWindow : Window
{
    private readonly FinanceiroService _financeiroService;
    private readonly CultureInfo _ptBr = new("pt-BR");
    private int _contaId;

    public QuitacaoContaWindow(FinanceiroService financeiroService)
    {
        _financeiroService = financeiroService;
        InitializeComponent();
        CmbForma.ItemsSource = Enum.GetValues<FormaPagamentoTipo>();
        CmbForma.SelectedIndex = 0;
        DtPagamento.SelectedDate = DateTime.Today;
    }

    public bool Abrir(Window owner, int contaId)
    {
        Owner = owner;
        _contaId = contaId;
        TxtValor.Text = "0,00";
        TxtJuros.Text = "0,00";
        TxtMulta.Text = "0,00";
        TxtDesconto.Text = "0,00";
        DtPagamento.SelectedDate = DateTime.Today;
        return ShowDialog() == true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        BtnConfirmar.IsEnabled = false;
        try
        {
            if (!decimal.TryParse(TxtValor.Text, NumberStyles.Any, _ptBr, out var valorPago) || valorPago <= 0)
            {
                MessageBox.Show("Valor pago inválido.", "Financeiro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal.TryParse(TxtJuros.Text, NumberStyles.Any, _ptBr, out var juros);
            decimal.TryParse(TxtMulta.Text, NumberStyles.Any, _ptBr, out var multa);
            decimal.TryParse(TxtDesconto.Text, NumberStyles.Any, _ptBr, out var desconto);
            var forma = CmbForma.SelectedItem is FormaPagamentoTipo fp ? fp : FormaPagamentoTipo.Dinheiro;

            var result = await _financeiroService.QuitarAsync(
                _contaId,
                DtPagamento.SelectedDate ?? DateTime.Today,
                valorPago,
                forma,
                juros,
                multa,
                desconto);

            if (!result.Sucesso)
            {
                MessageBox.Show(result.Erro ?? "Falha ao quitar conta.", "Financeiro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao quitar conta: {ex.Message}", "Financeiro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnConfirmar.IsEnabled = true;
        }
    }
}
