using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ProdutosWindow : UserControl
{
    private readonly ProdutoService _produtoService;
    private readonly ProdutoEditorWindow _produtoEditorWindow;
    private readonly CultureInfo _ptBr = new("pt-BR");
    private List<Produto> _produtos = [];

    public ProdutosWindow(ProdutoService produtoService, ProdutoEditorWindow produtoEditorWindow)
    {
        _produtoService = produtoService;
        _produtoEditorWindow = produtoEditorWindow;
        InitializeComponent();
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var filtro = TxtBusca.Text.Trim();
        _produtos = await _produtoService.ListarAsync(filtro);
        var ativos = _produtos.Count(p => p.Ativo);

        DgProdutos.ItemsSource = _produtos.Select(p => new ProdutoLinhaUi(
            p.Id,
            p.Codigo,
            p.CodigoBarras ?? "-",
            p.Descricao,
            p.Categoria?.Nome ?? "-",
            p.Unidade.ToString(),
            p.PrecoVenda.ToString("C", _ptBr),
            p.Estoque.ToString("N3", _ptBr),
            p.Ativo ? "Ativo" : "Inativo")).ToList();

        LblResumo.Text = $"{_produtos.Count} produto(s) listados | {ativos} ativo(s)";
    }

    private Produto? ObterSelecionado()
    {
        if (DgProdutos.SelectedItem is not ProdutoLinhaUi row)
            return null;
        return _produtos.FirstOrDefault(p => p.Id == row.Id);
    }

    private async void TxtBusca_TextChanged(object sender, TextChangedEventArgs e)
    {
        await CarregarAsync();
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }

    private async void Novo_Click(object sender, RoutedEventArgs e)
    {
        var novo = new Produto { Ativo = true };
        if (_produtoEditorWindow.Abrir(Window.GetWindow(this)!, novo))
            await CarregarAsync();
    }

    private async void Editar_Click(object sender, RoutedEventArgs e)
    {
        var produto = ObterSelecionado();
        if (produto == null)
        {
            MessageBox.Show("Selecione um produto para editar.", "Produtos", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var clone = new Produto
        {
            Id = produto.Id,
            Codigo = produto.Codigo,
            CodigoBarras = produto.CodigoBarras,
            Descricao = produto.Descricao,
            CategoriaId = produto.CategoriaId,
            Unidade = produto.Unidade,
            PrecoCusto = produto.PrecoCusto,
            PrecoVenda = produto.PrecoVenda,
            Estoque = produto.Estoque,
            EstoqueMinimo = produto.EstoqueMinimo,
            ControlaEstoque = produto.ControlaEstoque,
            PermiteVendaFracionada = produto.PermiteVendaFracionada,
            Ncm = produto.Ncm,
            Cest = produto.Cest,
            Cfop = produto.Cfop,
            Origem = produto.Origem,
            CstIcms = produto.CstIcms,
            AliquotaIcms = produto.AliquotaIcms,
            CstPisCofins = produto.CstPisCofins,
            Ativo = produto.Ativo
        };

        if (_produtoEditorWindow.Abrir(Window.GetWindow(this)!, clone))
            await CarregarAsync();
    }

    private async void Excluir_Click(object sender, RoutedEventArgs e)
    {
        var produto = ObterSelecionado();
        if (produto == null)
        {
            MessageBox.Show("Selecione um produto para excluir.", "Produtos", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ok = MessageBox.Show($"Confirma excluir o produto \"{produto.Descricao}\"?", "Produtos", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (ok != MessageBoxResult.Yes) return;

        var res = await _produtoService.ExcluirAsync(produto.Id);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao excluir.", "Produtos", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await CarregarAsync();
    }
}

public sealed record ProdutoLinhaUi(
    int Id,
    string Codigo,
    string CodigoBarras,
    string Descricao,
    string Categoria,
    string Unidade,
    string PrecoVenda,
    string Estoque,
    string Status);
