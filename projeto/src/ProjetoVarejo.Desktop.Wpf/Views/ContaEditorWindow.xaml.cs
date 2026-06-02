using System.Globalization;
using System.Windows;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ContaEditorWindow : Window
{
    private readonly FinanceiroService _financeiroService;
    private readonly ClienteService _clienteService;
    private readonly FornecedorService _fornecedorService;
    private readonly CultureInfo _ptBr = new("pt-BR");
    private ContaFinanceira _contaAtual = new();

    public ContaEditorWindow(FinanceiroService financeiroService, ClienteService clienteService, FornecedorService fornecedorService)
    {
        _financeiroService = financeiroService;
        _clienteService = clienteService;
        _fornecedorService = fornecedorService;
        InitializeComponent();
        CmbTipo.ItemsSource = Enum.GetValues<TipoConta>();
    }

    public bool Abrir(Window owner, ContaFinanceira conta)
    {
        Owner = owner;
        _contaAtual = conta;
        _ = CarregarListasAsync();

        CmbTipo.SelectedItem = conta.Tipo;
        TxtValor.Text = conta.Valor.ToString("N2", _ptBr);
        TxtDescricao.Text = conta.Descricao;
        TxtDocumento.Text = conta.DocumentoNumero ?? string.Empty;
        DtEmissao.SelectedDate = conta.DataEmissao == default ? DateTime.Today : conta.DataEmissao;
        DtVencimento.SelectedDate = conta.DataVencimento == default ? DateTime.Today.AddDays(30) : conta.DataVencimento;
        TxtObservacao.Text = conta.Observacao ?? string.Empty;

        return ShowDialog() == true;
    }

    private async Task CarregarListasAsync()
    {
        var clientes = await _clienteService.ListarAsync();
        var fornecedores = await _fornecedorService.ListarAsync();

        CmbCliente.DisplayMemberPath = "Nome";
        CmbCliente.SelectedValuePath = "Id";
        CmbCliente.ItemsSource = new[] { new ClienteRef("(nenhum)", null) }
            .Concat(clientes.Select(c => new ClienteRef(c.Nome, c.Id)))
            .ToList();
        CmbCliente.SelectedValue = _contaAtual.ClienteId;
        if (CmbCliente.SelectedIndex < 0) CmbCliente.SelectedIndex = 0;

        CmbFornecedor.DisplayMemberPath = "Nome";
        CmbFornecedor.SelectedValuePath = "Id";
        CmbFornecedor.ItemsSource = new[] { new FornecedorRef("(nenhum)", null) }
            .Concat(fornecedores.Select(f => new FornecedorRef(f.RazaoSocial, f.Id)))
            .ToList();
        CmbFornecedor.SelectedValue = _contaAtual.FornecedorId;
        if (CmbFornecedor.SelectedIndex < 0) CmbFornecedor.SelectedIndex = 0;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void Salvar_Click(object sender, RoutedEventArgs e)
    {
        BtnSalvar.IsEnabled = false;
        BtnSalvar.Content = "Salvando...";
        try
        {
            if (!decimal.TryParse(TxtValor.Text, NumberStyles.Any, _ptBr, out var valor))
            {
                MessageBox.Show("Valor inválido.", "Financeiro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _contaAtual.Tipo = CmbTipo.SelectedItem is TipoConta tipo ? tipo : TipoConta.Pagar;
            _contaAtual.Descricao = TxtDescricao.Text.Trim();
            _contaAtual.DocumentoNumero = TxtDocumento.Text.Trim();
            _contaAtual.DataEmissao = DtEmissao.SelectedDate ?? DateTime.Today;
            _contaAtual.DataVencimento = DtVencimento.SelectedDate ?? DateTime.Today.AddDays(30);
            _contaAtual.Valor = valor;
            _contaAtual.ClienteId = CmbCliente.SelectedItem is ClienteRef c ? c.Id : null;
            _contaAtual.FornecedorId = CmbFornecedor.SelectedItem is FornecedorRef f ? f.Id : null;
            _contaAtual.Observacao = TxtObservacao.Text.Trim();
            if (_contaAtual.Id == 0 && _contaAtual.Status == 0)
                _contaAtual.Status = StatusConta.EmAberto;

            var result = await _financeiroService.SalvarAsync(_contaAtual);
            if (!result.Sucesso)
            {
                MessageBox.Show(result.Erro ?? "Falha ao salvar conta.", "Financeiro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar conta: {ex.Message}", "Financeiro", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnSalvar.IsEnabled = true;
            BtnSalvar.Content = "Salvar";
        }
    }
}

// ToString retorna o texto visível — o tema WPF não popula a SelectionBoxItemTemplate
// a partir de DisplayMemberPath quando a caixa está fechada; sem ToString() apareceria
// o nome completo do tipo.
public sealed record ClienteRef(string Nome, int? Id)
{
    public override string ToString() => Nome;
}

public sealed record FornecedorRef(string Nome, int? Id)
{
    public override string ToString() => Nome;
}
