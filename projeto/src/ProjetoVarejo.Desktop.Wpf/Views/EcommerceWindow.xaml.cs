using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class EcommerceWindow : UserControl
{
    private readonly AppDbContext _db;
    private readonly CultureInfo _ptBr = new("pt-BR");

    public EcommerceWindow(AppDbContext db)
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

        var vendas = await _db.Vendas
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Where(v => v.DataVenda >= de && v.DataVenda < ate && v.Status == StatusVenda.Finalizada)
            .ToListAsync();

        var pedidos = vendas.Count;
        var faturamento = vendas.Sum(v => v.Total);
        var ticket = pedidos == 0 ? 0 : faturamento / pedidos;
        var clientesCompradores = vendas.Where(v => v.ClienteId != null).Select(v => v.ClienteId!.Value).Distinct().Count();

        LblPedidos.Text = pedidos.ToString("N0", _ptBr);
        LblFaturamento.Text = faturamento.ToString("C", _ptBr);
        LblTicket.Text = ticket.ToString("C", _ptBr);
        LblClientes.Text = clientesCompradores.ToString("N0", _ptBr);

        var topProdutos = await _db.ItensVenda
            .AsNoTracking()
            .Include(i => i.Produto)
            .Include(i => i.Venda)
            .Where(i => i.Venda.DataVenda >= de && i.Venda.DataVenda < ate && i.Venda.Status == StatusVenda.Finalizada)
            .GroupBy(i => new { i.ProdutoId, i.Produto.Descricao })
            .Select(g => new
            {
                Produto = g.Key.Descricao,
                Quantidade = g.Sum(x => x.Quantidade),
                Faturamento = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.Faturamento)
            .Take(100)
            .ToListAsync();

        DgTopProdutos.ItemsSource = topProdutos.Select(x => new
        {
            x.Produto,
            Quantidade = x.Quantidade.ToString("N3", _ptBr),
            Faturamento = x.Faturamento.ToString("C", _ptBr)
        }).ToList();

        DgClientes.ItemsSource = vendas
            .Where(v => v.ClienteId != null)
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente!.Nome })
            .Select(g => new
            {
                Cliente = g.Key.Nome,
                Pedidos = g.Count(),
                Total = g.Sum(x => x.Total),
                UltimaCompra = g.Max(x => x.DataVenda)
            })
            .OrderByDescending(x => x.Total)
            .Take(100)
            .Select(x => new
            {
                x.Cliente,
                Pedidos = x.Pedidos.ToString("N0", _ptBr),
                Total = x.Total.ToString("C", _ptBr),
                UltimaCompra = x.UltimaCompra.ToString("dd/MM/yyyy HH:mm")
            })
            .ToList();
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }
}
