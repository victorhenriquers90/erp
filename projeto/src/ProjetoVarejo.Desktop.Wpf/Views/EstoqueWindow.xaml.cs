using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class EstoqueWindow : UserControl
{
    private readonly EstoqueService _estoqueService;
    private readonly LancamentoEstoqueWindow _lancamentoWindow;
    private readonly CultureInfo _ptBr = new("pt-BR");

    public EstoqueWindow(EstoqueService estoqueService, LancamentoEstoqueWindow lancamentoWindow)
    {
        _estoqueService = estoqueService;
        _lancamentoWindow = lancamentoWindow;
        InitializeComponent();
        DtDe.SelectedDate = DateTime.Today.AddDays(-30);
        DtAte.SelectedDate = DateTime.Today;
        Loaded += async (_, _) =>
        {
            await CarregarMovimentosAsync();
            await CarregarMinimoAsync();
        };
    }

    private async Task CarregarMovimentosAsync()
    {
        var de = DtDe.SelectedDate ?? DateTime.Today.AddDays(-30);
        var ate = (DtAte.SelectedDate ?? DateTime.Today).AddDays(1);
        var filtro = TxtFiltroProduto.Text.Trim();

        var movimentos = await _estoqueService.ListarMovimentosAsync(null, de, ate);
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            movimentos = movimentos.Where(m =>
                m.Produto.Descricao.Contains(filtro, StringComparison.OrdinalIgnoreCase) ||
                m.Produto.Codigo.Contains(filtro, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        DgMovimentos.ItemsSource = movimentos.Select(m => new MovimentoLinhaUi(
            m.CriadoEm.ToString("dd/MM/yyyy HH:mm"),
            m.Tipo.ToString(),
            m.Produto.Descricao,
            m.Quantidade.ToString("N3", _ptBr),
            m.SaldoAnterior.ToString("N3", _ptBr),
            m.SaldoAtual.ToString("N3", _ptBr),
            m.Documento ?? "-",
            m.Usuario.Nome)).ToList();
    }

    private async Task CarregarMinimoAsync()
    {
        var produtos = await _estoqueService.ProdutosAbaixoMinimoAsync();
        DgMinimo.ItemsSource = produtos.Select(p => new MinimoLinhaUi(
            p.Codigo,
            p.Descricao,
            p.Estoque.ToString("N3", _ptBr),
            p.EstoqueMinimo.ToString("N3", _ptBr))).ToList();
    }

    private async void Entrada_Click(object sender, RoutedEventArgs e)
    {
        if (_lancamentoWindow.Abrir(Window.GetWindow(this)!, TipoMovimentoEstoque.Entrada))
        {
            await CarregarMovimentosAsync();
            await CarregarMinimoAsync();
        }
    }

    private async void Saida_Click(object sender, RoutedEventArgs e)
    {
        if (_lancamentoWindow.Abrir(Window.GetWindow(this)!, TipoMovimentoEstoque.AjusteSaida))
        {
            await CarregarMovimentosAsync();
            await CarregarMinimoAsync();
        }
    }

    private async void AtualizarMov_Click(object sender, RoutedEventArgs e)
    {
        await CarregarMovimentosAsync();
    }

    private async void FiltroDataMudou(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await CarregarMovimentosAsync();
    }

    private async void FiltroTextoMudou(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        await CarregarMovimentosAsync();
    }
}

public sealed record MovimentoLinhaUi(
    string Data,
    string Tipo,
    string Produto,
    string Quantidade,
    string SaldoAnterior,
    string SaldoAtual,
    string Documento,
    string Usuario);

public sealed record MinimoLinhaUi(
    string Codigo,
    string Descricao,
    string Estoque,
    string Minimo);
