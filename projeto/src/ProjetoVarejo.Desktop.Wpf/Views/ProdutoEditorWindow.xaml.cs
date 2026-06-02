using System.Globalization;
using System.Windows;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ProdutoEditorWindow : Window
{
    private readonly ProdutoService _produtoService;
    private readonly CategoriaService _categoriaService;
    private readonly CultureInfo _ptBr = new("pt-BR");
    private Produto _produtoAtual = new();
    private List<Categoria> _categorias = [];

    public ProdutoEditorWindow(ProdutoService produtoService, CategoriaService categoriaService)
    {
        _produtoService = produtoService;
        _categoriaService = categoriaService;
        InitializeComponent();
        CmbUnidade.ItemsSource = Enum.GetValues<UnidadeMedida>();
    }

    public bool Abrir(Window owner, Produto produto)
    {
        Owner = owner;
        _produtoAtual = produto;
        _ = CarregarCategoriasAsync();

        TxtCodigo.Text = produto.Codigo;
        TxtCodigoBarras.Text = produto.CodigoBarras ?? string.Empty;
        TxtDescricao.Text = produto.Descricao;
        CmbUnidade.SelectedItem = produto.Unidade;
        TxtPrecoCusto.Text = produto.PrecoCusto.ToString("N2", _ptBr);
        TxtPrecoVenda.Text = produto.PrecoVenda.ToString("N2", _ptBr);
        TxtEstoque.Text = produto.Estoque.ToString("N3", _ptBr);
        TxtEstoqueMinimo.Text = produto.EstoqueMinimo.ToString("N3", _ptBr);
        ChkControlaEstoque.IsChecked = produto.ControlaEstoque;
        ChkVendaFracionada.IsChecked = produto.PermiteVendaFracionada;
        ChkAtivo.IsChecked = produto.Ativo;

        return ShowDialog() == true;
    }

    private async Task CarregarCategoriasAsync()
    {
        _categorias = await _categoriaService.ListarAsync();
        CmbCategoria.DisplayMemberPath = null;
        CmbCategoria.ItemsSource = _categorias.Select(c => new CategoriaOpcao(c.Id, c.Nome)).ToList();
        CmbCategoria.SelectedValuePath = "Id";
        CmbCategoria.SelectedValue = _produtoAtual.CategoriaId;
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
            _produtoAtual.Codigo = TxtCodigo.Text.Trim();
            _produtoAtual.CodigoBarras = TxtCodigoBarras.Text.Trim();
            _produtoAtual.Descricao = TxtDescricao.Text.Trim();
            _produtoAtual.CategoriaId = CmbCategoria.SelectedValue is int categoriaId ? categoriaId : null;
            _produtoAtual.Unidade = CmbUnidade.SelectedItem is UnidadeMedida un ? un : UnidadeMedida.UN;
            _produtoAtual.PrecoCusto = ParseDecimal(TxtPrecoCusto.Text);
            _produtoAtual.PrecoVenda = ParseDecimal(TxtPrecoVenda.Text);
            _produtoAtual.Estoque = ParseDecimal(TxtEstoque.Text);
            _produtoAtual.EstoqueMinimo = ParseDecimal(TxtEstoqueMinimo.Text);
            _produtoAtual.ControlaEstoque = ChkControlaEstoque.IsChecked == true;
            _produtoAtual.PermiteVendaFracionada = ChkVendaFracionada.IsChecked == true;
            _produtoAtual.Ativo = ChkAtivo.IsChecked == true;

            var result = await _produtoService.SalvarAsync(_produtoAtual);
            if (!result.Sucesso)
            {
                MessageBox.Show(result.Erro ?? "Falha ao salvar.", "Produtos", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar produto: {ex.Message}", "Produtos", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnSalvar.IsEnabled = true;
            BtnSalvar.Content = "Salvar";
        }
    }

    private decimal ParseDecimal(string raw)
    {
        if (decimal.TryParse(raw, NumberStyles.Any, _ptBr, out var v))
            return v;
        if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
            return v;
        return 0;
    }
}

// ToString retorna o texto visível — o tema WPF não popula a SelectionBoxItemTemplate
// a partir de DisplayMemberPath quando a caixa está fechada; sem ToString() apareceria
// o nome completo do tipo (ex.: "ProjetoVarejo.Domain.Entities.Categoria").
public sealed record CategoriaOpcao(int Id, string Nome)
{
    public override string ToString() => Nome;
}
