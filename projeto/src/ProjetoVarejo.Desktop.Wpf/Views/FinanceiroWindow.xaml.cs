using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class FinanceiroWindow : UserControl
{
    private readonly FinanceiroService _financeiroService;
    private readonly ContaEditorWindow _contaEditorWindow;
    private readonly QuitacaoContaWindow _quitacaoContaWindow;
    private readonly CultureInfo _ptBr = new("pt-BR");
    private List<ContaFinanceira> _contas = [];

    public FinanceiroWindow(FinanceiroService financeiroService, ContaEditorWindow contaEditorWindow, QuitacaoContaWindow quitacaoContaWindow)
    {
        _financeiroService = financeiroService;
        _contaEditorWindow = contaEditorWindow;
        _quitacaoContaWindow = quitacaoContaWindow;
        InitializeComponent();
        CarregarCombos();
        DtDe.SelectedDate = DateTime.Today.AddDays(-30);
        DtAte.SelectedDate = DateTime.Today.AddDays(60);
        Loaded += async (_, _) => await CarregarAsync();
    }

    private void CarregarCombos()
    {
        CmbTipo.ItemsSource = new List<TipoOpcao>
        {
            new("Todos", null),
            new("Pagar", TipoConta.Pagar),
            new("Receber", TipoConta.Receber)
        };
        CmbTipo.DisplayMemberPath = "Nome";
        CmbTipo.SelectedValuePath = "Valor";
        CmbTipo.SelectedIndex = 0;

        CmbStatus.ItemsSource = new List<StatusOpcao>
        {
            new("Todos", null),
            new("Em aberto", StatusConta.EmAberto),
            new("Paga", StatusConta.Paga),
            new("Atrasada", StatusConta.Atrasada),
            new("Cancelada", StatusConta.Cancelada)
        };
        CmbStatus.DisplayMemberPath = "Nome";
        CmbStatus.SelectedValuePath = "Valor";
        CmbStatus.SelectedIndex = 0;
    }

    private async Task CarregarAsync()
    {
        var tipo = CmbTipo.SelectedItem is TipoOpcao t ? t.Valor : null;
        var status = CmbStatus.SelectedItem is StatusOpcao s ? s.Valor : null;
        var de = DtDe.SelectedDate ?? DateTime.Today.AddDays(-30);
        var ate = (DtAte.SelectedDate ?? DateTime.Today.AddDays(60)).AddDays(1);

        _contas = await _financeiroService.ListarAsync(tipo, status, de, ate);
        DgFinanceiro.ItemsSource = _contas.Select(c => new ContaLinhaUi(
            c.Id,
            c.Tipo.ToString(),
            c.Descricao,
            c.Cliente?.Nome ?? c.Fornecedor?.RazaoSocial ?? "-",
            c.DataVencimento.ToString("dd/MM/yyyy"),
            c.Valor.ToString("C", _ptBr),
            c.ValorPago.ToString("C", _ptBr),
            c.Status.ToString())).ToList();

        var (rec, pag, saldo) = await _financeiroService.ResumoAsync(de, ate);
        LblReceber.Text = rec.ToString("C", _ptBr);
        LblPagar.Text = pag.ToString("C", _ptBr);
        LblSaldo.Text = saldo.ToString("C", _ptBr);
        LblMovimentos.Text = _contas.Count.ToString("N0", _ptBr);
    }

    private ContaFinanceira? ContaSelecionada()
    {
        if (DgFinanceiro.SelectedItem is not ContaLinhaUi row)
            return null;
        return _contas.FirstOrDefault(c => c.Id == row.Id);
    }

    private async void NovaConta_Click(object sender, RoutedEventArgs e)
    {
        var conta = new ContaFinanceira
        {
            DataEmissao = DateTime.Today,
            DataVencimento = DateTime.Today.AddDays(30),
            Tipo = TipoConta.Pagar,
            Status = StatusConta.EmAberto
        };
        if (_contaEditorWindow.Abrir(Window.GetWindow(this)!, conta))
            await CarregarAsync();
    }

    private async void DgFinanceiro_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var conta = ContaSelecionada();
        if (conta == null) return;

        var clone = new ContaFinanceira
        {
            Id = conta.Id,
            Tipo = conta.Tipo,
            Descricao = conta.Descricao,
            DocumentoNumero = conta.DocumentoNumero,
            DataEmissao = conta.DataEmissao,
            DataVencimento = conta.DataVencimento,
            DataPagamento = conta.DataPagamento,
            Valor = conta.Valor,
            ValorPago = conta.ValorPago,
            Juros = conta.Juros,
            Multa = conta.Multa,
            Desconto = conta.Desconto,
            Status = conta.Status,
            FormaPagamento = conta.FormaPagamento,
            ClienteId = conta.ClienteId,
            FornecedorId = conta.FornecedorId,
            VendaId = conta.VendaId,
            Observacao = conta.Observacao,
            Ativo = conta.Ativo
        };

        if (_contaEditorWindow.Abrir(Window.GetWindow(this)!, clone))
            await CarregarAsync();
    }

    private async void Quitar_Click(object sender, RoutedEventArgs e)
    {
        var conta = ContaSelecionada();
        if (conta == null)
        {
            MessageBox.Show("Selecione uma conta para quitar.", "Financeiro", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_quitacaoContaWindow.Abrir(Window.GetWindow(this)!, conta.Id))
            await CarregarAsync();
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }

    private async void FiltroSelecionadoMudou(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await CarregarAsync();
    }

    private async void FiltroDataMudou(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await CarregarAsync();
    }
}

// ToString retorna o Nome: o template de ComboBox do tema não popula SelectionBoxItemTemplate
// a partir do DisplayMemberPath, então a caixa de seleção fechada cai no ToString() do record.
// Sem isto a seleção exibiria "TipoOpcao { Nome = ... }" em vez do texto amigável.
public sealed record TipoOpcao(string Nome, TipoConta? Valor)
{
    public override string ToString() => Nome;
}

public sealed record StatusOpcao(string Nome, StatusConta? Valor)
{
    public override string ToString() => Nome;
}
public sealed record ContaLinhaUi(
    int Id,
    string Tipo,
    string Descricao,
    string Pessoa,
    string Vencimento,
    string Valor,
    string Pago,
    string Status);
