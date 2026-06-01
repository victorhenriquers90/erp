using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ProdutoBuscaWindow : Window
{
    private readonly ProdutoService _produtoService;
    private readonly CultureInfo _ptBr = new("pt-BR");
    private List<Produto> _produtos = [];
    private Produto? _selecionado;

    public ProdutoBuscaWindow(ProdutoService produtoService)
    {
        _produtoService = produtoService;
        InitializeComponent();
        Loaded += async (_, _) => await CarregarAsync();
    }

    public bool Abrir(Window owner, out Produto? produtoSelecionado)
    {
        Owner = owner;
        TxtFiltro.Text = string.Empty;
        _selecionado = null;
        var ok = ShowDialog() == true;
        produtoSelecionado = _selecionado;
        return ok;
    }

    private async Task CarregarAsync()
    {
        _produtos = await _produtoService.ListarAsync(TxtFiltro.Text.Trim());
        DgProdutos.ItemsSource = _produtos.Select(p => new ProdutoBuscaLinhaUi(
            p.Id,
            p.Codigo,
            p.CodigoBarras ?? "-",
            p.Descricao,
            p.Categoria?.Nome ?? "-",
            p.PrecoVenda.ToString("C", _ptBr),
            p.Estoque.ToString("N3", _ptBr))).ToList();
    }

    private Produto? SelecionadoAtual()
    {
        if (DgProdutos.SelectedItem is not ProdutoBuscaLinhaUi row) return null;
        return _produtos.FirstOrDefault(p => p.Id == row.Id);
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }

    private async void TxtFiltro_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await CarregarAsync();
    }

    private void DgProdutos_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ConfirmarSelecao();
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Selecionar_Click(object sender, RoutedEventArgs e)
    {
        ConfirmarSelecao();
    }

    private void ConfirmarSelecao()
    {
        var produto = SelecionadoAtual();
        if (produto == null)
        {
            MessageBox.Show("Selecione um produto.", "Busca de Produto", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        _selecionado = produto;
        DialogResult = true;
        Close();
    }
}

public sealed record ProdutoBuscaLinhaUi(
    int Id,
    string Codigo,
    string CodigoBarras,
    string Descricao,
    string Categoria,
    string Preco,
    string Estoque);
