using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ProducaoWindow : UserControl
{
    private readonly AppDbContext _db;
    private readonly CultureInfo _ptBr = new("pt-BR");

    public ProducaoWindow(AppDbContext db)
    {
        _db = db;
        InitializeComponent();
        DtDe.SelectedDate = DateTime.Today.AddDays(-30);
        DtAte.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var de = DtDe.SelectedDate ?? DateTime.Today.AddDays(-30);
        var ate = (DtAte.SelectedDate ?? DateTime.Today).AddDays(1);

        var movimentos = await _db.MovimentosEstoque
            .AsNoTracking()
            .Include(m => m.Produto)
            .Where(m => m.CriadoEm >= de && m.CriadoEm < ate)
            .ToListAsync();

        var entradasTipos = new[] { TipoMovimentoEstoque.Entrada, TipoMovimentoEstoque.AjusteEntrada, TipoMovimentoEstoque.Devolucao };
        var saidasTipos = new[] { TipoMovimentoEstoque.Saida, TipoMovimentoEstoque.AjusteSaida };

        var entradas = movimentos
            .Where(m => entradasTipos.Contains(m.Tipo))
            .Sum(m => m.Quantidade);
        var saidas = movimentos
            .Where(m => saidasTipos.Contains(m.Tipo))
            .Sum(m => m.Quantidade);

        var ordensAbertas = await _db.Vendas
            .AsNoTracking()
            .CountAsync(v => v.Status == StatusVenda.EmAberto);

        LblOrdensAbertas.Text = ordensAbertas.ToString("N0", _ptBr);
        LblEntradas.Text = entradas.ToString("N3", _ptBr);
        LblSaidas.Text = saidas.ToString("N3", _ptBr);

        var porProduto = movimentos
            .GroupBy(m => new { m.ProdutoId, Nome = m.Produto.Descricao })
            .Select(g =>
            {
                var totalEntradas = g.Where(x => entradasTipos.Contains(x.Tipo)).Sum(x => x.Quantidade);
                var totalSaidas = g.Where(x => saidasTipos.Contains(x.Tipo)).Sum(x => x.Quantidade);
                var ultima = g.Max(x => x.CriadoEm);
                var saldo = g.OrderByDescending(x => x.CriadoEm).First().SaldoAtual;
                return new
                {
                    Produto = g.Key.Nome,
                    Entradas = totalEntradas,
                    Saidas = totalSaidas,
                    Saldo = saldo,
                    Ultima = ultima
                };
            })
            .OrderByDescending(x => x.Ultima)
            .Take(300)
            .Select(x => new
            {
                x.Produto,
                Entradas = x.Entradas.ToString("N3", _ptBr),
                Saidas = x.Saidas.ToString("N3", _ptBr),
                Saldo = x.Saldo.ToString("N3", _ptBr),
                UltimaMovimentacao = x.Ultima.ToString("dd/MM/yyyy HH:mm")
            })
            .ToList();

        DgProducao.ItemsSource = porProduto;
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }
}
