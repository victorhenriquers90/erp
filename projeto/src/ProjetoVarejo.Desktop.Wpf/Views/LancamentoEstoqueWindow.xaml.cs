using System.Globalization;
using System.Windows;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class LancamentoEstoqueWindow : Window
{
    private readonly EstoqueService _estoqueService;
    private readonly ProdutoService _produtoService;
    private readonly FornecedorService _fornecedorService;
    private readonly CultureInfo _ptBr = new("pt-BR");
    private TipoMovimentoEstoque _tipo;
    private Produto? _produtoSelecionado;

    public LancamentoEstoqueWindow(EstoqueService estoqueService, ProdutoService produtoService, FornecedorService fornecedorService)
    {
        _estoqueService = estoqueService;
        _produtoService = produtoService;
        _fornecedorService = fornecedorService;
        InitializeComponent();
    }

    public bool Abrir(Window owner, TipoMovimentoEstoque tipo)
    {
        Owner = owner;
        _tipo = tipo;
        _produtoSelecionado = null;
        TxtCodigo.Text = string.Empty;
        TxtQuantidade.Text = "1";
        TxtCusto.Text = "0,00";
        TxtDocumento.Text = string.Empty;
        TxtObservacao.Text = string.Empty;
        LblProduto.Text = "(nenhum produto selecionado)";
        LblProduto.Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextSoft"];
        LblTitulo.Text = tipo == TipoMovimentoEstoque.Entrada ? "Lançar entrada de estoque" : "Ajustar saída de estoque";
        BtnSalvar.Content = tipo == TipoMovimentoEstoque.Entrada ? "Lançar entrada" : "Lançar saída";
        BtnSalvar.Background = tipo == TipoMovimentoEstoque.Entrada
            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(31, 157, 85))
            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(214, 69, 69));
        _ = CarregarFornecedoresAsync(tipo == TipoMovimentoEstoque.Entrada);
        return ShowDialog() == true;
    }

    private async Task CarregarFornecedoresAsync(bool exibir)
    {
        CmbFornecedor.Items.Clear();
        CmbFornecedor.Items.Add(new FornecedorOpcao("(nenhum)", null));
        CmbFornecedor.IsEnabled = exibir;
        CmbFornecedor.DisplayMemberPath = "Nome";
        CmbFornecedor.SelectedValuePath = "Id";
        CmbFornecedor.SelectedIndex = 0;
        if (!exibir) return;
        var lista = await _fornecedorService.ListarAsync();
        foreach (var item in lista)
            CmbFornecedor.Items.Add(new FornecedorOpcao(item.RazaoSocial, item.Id));
    }

    private async void Buscar_Click(object sender, RoutedEventArgs e)
    {
        await BuscarProdutoAsync();
    }

    private async Task BuscarProdutoAsync()
    {
        var codigo = TxtCodigo.Text.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
            return;

        _produtoSelecionado = await _produtoService.BuscarPorCodigoAsync(codigo);
        if (_produtoSelecionado == null)
        {
            LblProduto.Text = "(produto não encontrado)";
            LblProduto.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 35, 24));
            return;
        }

        LblProduto.Text = $"{_produtoSelecionado.Codigo} - {_produtoSelecionado.Descricao}";
        LblProduto.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 118, 70));
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void Salvar_Click(object sender, RoutedEventArgs e)
    {
        BtnSalvar.IsEnabled = false;
        try
        {
            if (_produtoSelecionado == null)
            {
                MessageBox.Show("Selecione um produto válido.", "Estoque", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtQuantidade.Text, NumberStyles.Any, _ptBr, out var qtd) || qtd <= 0)
            {
                MessageBox.Show("Quantidade inválida.", "Estoque", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal? custo = null;
            if (_tipo == TipoMovimentoEstoque.Entrada && decimal.TryParse(TxtCusto.Text, NumberStyles.Any, _ptBr, out var custoParse) && custoParse > 0)
                custo = custoParse;

            var fornecedorId = CmbFornecedor.SelectedItem is FornecedorOpcao fornecedor ? fornecedor.Id : null;
            var res = await _estoqueService.RegistrarMovimentoAsync(
                _produtoSelecionado.Id,
                _tipo,
                qtd,
                custo,
                string.IsNullOrWhiteSpace(TxtDocumento.Text) ? null : TxtDocumento.Text.Trim(),
                null,
                fornecedorId,
                string.IsNullOrWhiteSpace(TxtObservacao.Text) ? null : TxtObservacao.Text.Trim());

            if (!res.Sucesso)
            {
                MessageBox.Show(res.Erro ?? "Falha ao lançar movimento.", "Estoque", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao lançar movimento: {ex.Message}", "Estoque", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnSalvar.IsEnabled = true;
        }
    }
}

// ToString retorna o texto visível — o tema WPF não popula a SelectionBoxItemTemplate
// a partir de DisplayMemberPath quando a caixa está fechada; sem ToString() apareceria
// o nome completo do tipo.
public sealed record FornecedorOpcao(string Nome, int? Id)
{
    public override string ToString() => Nome;
}
