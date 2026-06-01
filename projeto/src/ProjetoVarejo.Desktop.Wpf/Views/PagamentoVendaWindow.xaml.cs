using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class PagamentoVendaWindow : Window
{
    private readonly CultureInfo _ptBr = new("pt-BR");
    private decimal _total;
    private readonly List<PagamentoVenda> _pagamentos = [];

    public PagamentoVendaWindow()
    {
        InitializeComponent();
        CmbForma.ItemsSource = Enum.GetValues<FormaPagamentoTipo>();
        CmbForma.SelectedIndex = 0;
        PreviewKeyDown += PagamentoVendaWindow_PreviewKeyDown;
    }

    public bool Abrir(Window owner, decimal total, out List<PagamentoVenda> pagamentos)
    {
        Owner = owner;
        _total = total;
        _pagamentos.Clear();
        DgPagamentos.ItemsSource = null;
        LblTotal.Text = _total.ToString("C", _ptBr);
        TxtValor.Text = _total.ToString("N2", _ptBr);
        AtualizarSaldos();
        var ok = ShowDialog() == true;
        pagamentos = _pagamentos.Select(p => new PagamentoVenda
        {
            FormaPagamento = p.FormaPagamento,
            Valor = p.Valor,
            Parcelas = p.Parcelas,
            Autorizacao = p.Autorizacao
        }).ToList();
        return ok;
    }

    private void PagamentoVendaWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F10 && BtnConfirmar.IsEnabled)
        {
            DialogResult = true;
            Close();
        }
    }

    private void Adicionar_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtValor.Text, NumberStyles.Any, _ptBr, out var valor) || valor <= 0)
        {
            MessageBox.Show("Valor inválido.", "Pagamento", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (CmbForma.SelectedItem is not FormaPagamentoTipo forma)
            return;

        _pagamentos.Add(new PagamentoVenda { FormaPagamento = forma, Valor = valor });
        DgPagamentos.ItemsSource = _pagamentos.Select(p => new PagamentoLinhaUi(
            p.FormaPagamento.ToString(),
            p.Valor.ToString("C", _ptBr))).ToList();

        var falta = _total - _pagamentos.Sum(x => x.Valor);
        TxtValor.Text = (falta > 0 ? falta : 0).ToString("N2", _ptBr);
        AtualizarSaldos();
    }

    private void DgPagamentos_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete) return;
        if (DgPagamentos.SelectedIndex < 0 || DgPagamentos.SelectedIndex >= _pagamentos.Count) return;
        _pagamentos.RemoveAt(DgPagamentos.SelectedIndex);
        DgPagamentos.ItemsSource = _pagamentos.Select(p => new PagamentoLinhaUi(
            p.FormaPagamento.ToString(),
            p.Valor.ToString("C", _ptBr))).ToList();
        AtualizarSaldos();
    }

    private void AtualizarSaldos()
    {
        var pago = _pagamentos.Sum(p => p.Valor);
        var falta = _total - pago;
        if (falta > 0)
        {
            LblFalta.Text = falta.ToString("C", _ptBr);
            LblTroco.Text = 0m.ToString("C", _ptBr);
            BtnConfirmar.IsEnabled = false;
            return;
        }

        LblFalta.Text = 0m.ToString("C", _ptBr);
        LblTroco.Text = Math.Abs(falta).ToString("C", _ptBr);
        BtnConfirmar.IsEnabled = _pagamentos.Count > 0;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        if (!BtnConfirmar.IsEnabled) return;
        DialogResult = true;
        Close();
    }
}

public sealed record PagamentoLinhaUi(string Forma, string Valor);
