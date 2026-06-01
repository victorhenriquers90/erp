using System.Globalization;
using System.Text;
using System.Windows;
using ProjetoVarejo.Application.Services;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class CaixaFechamentoWindow : Window
{
    private readonly CultureInfo _ptBr = new("pt-BR");
    private decimal _valorInformado;
    private string _observacao = string.Empty;

    public CaixaFechamentoWindow()
    {
        InitializeComponent();
    }

    public bool Abrir(Window owner, CaixaService.ResumoCaixa resumo, out decimal valorInformado, out string? observacao)
    {
        Owner = owner;

        var sb = new StringBuilder();
        sb.AppendLine($"Abertura............. {resumo.ValorAbertura.ToString("C", _ptBr)}");
        sb.AppendLine($"Suprimentos.......... {resumo.TotalSuprimentos.ToString("C", _ptBr)}");
        sb.AppendLine($"Sangrias............. {resumo.TotalSangrias.ToString("C", _ptBr)}");
        sb.AppendLine($"Vendas em dinheiro... {(resumo.VendasPorForma.TryGetValue(Shared.FormaPagamentoTipo.Dinheiro, out var din) ? din : 0).ToString("C", _ptBr)}");
        sb.AppendLine($"Esperado em caixa.... {resumo.SaldoDinheiroEsperado.ToString("C", _ptBr)}");
        LblResumo.Text = sb.ToString();

        TxtValorInformado.Text = resumo.SaldoDinheiroEsperado.ToString("N2", _ptBr);
        TxtObservacao.Text = string.Empty;
        _valorInformado = 0;
        _observacao = string.Empty;

        var ok = ShowDialog() == true;
        valorInformado = _valorInformado;
        observacao = string.IsNullOrWhiteSpace(_observacao) ? null : _observacao;
        return ok;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(TxtValorInformado.Text, NumberStyles.Any, _ptBr, out var valor))
        {
            MessageBox.Show("Valor informado invalido.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _valorInformado = valor;
        _observacao = TxtObservacao.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void CopiarResumo_Click(object sender, RoutedEventArgs e)
    {
        var texto = LblResumo.Text?.Trim();
        if (string.IsNullOrWhiteSpace(texto))
        {
            MessageBox.Show("Resumo vazio.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(texto);
        MessageBox.Show("Resumo copiado para a area de transferencia.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
