using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class PedidosWindow : UserControl
{
    private readonly AppDbContext _db;
    private readonly CultureInfo _ptBr = new("pt-BR");

    public PedidosWindow(AppDbContext db)
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
        var busca = (TxtBusca.Text ?? string.Empty).Trim();

        var query = _db.Vendas
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Include(v => v.Usuario)
            .Include(v => v.Itens)
            .Where(v => v.DataVenda >= de && v.DataVenda < ate);

        if (!string.IsNullOrWhiteSpace(busca))
        {
            query = query.Where(v =>
                v.Numero.Contains(busca)
                || (v.Cliente != null && v.Cliente.Nome.Contains(busca)));
        }

        query = CmbStatus.SelectedIndex switch
        {
            1 => query.Where(v => v.Status == StatusVenda.EmAberto),
            2 => query.Where(v => v.Status == StatusVenda.Finalizada),
            3 => query.Where(v => v.Status == StatusVenda.Cancelada),
            _ => query
        };

        var vendas = await query
            .OrderByDescending(v => v.DataVenda)
            .Take(500)
            .ToListAsync();

        LblTotalPedidos.Text = vendas.Count.ToString("N0", _ptBr);
        LblFinalizados.Text = vendas.Count(v => v.Status == StatusVenda.Finalizada).ToString("N0", _ptBr);
        LblEmAberto.Text = vendas.Count(v => v.Status == StatusVenda.EmAberto).ToString("N0", _ptBr);
        LblValorPeriodo.Text = vendas.Sum(v => v.Total).ToString("C", _ptBr);

        DgPedidos.ItemsSource = vendas.Select(v => new
        {
            Numero = string.IsNullOrWhiteSpace(v.Numero) ? v.Id.ToString("D6") : v.Numero,
            Data = v.DataVenda.ToString("dd/MM/yyyy HH:mm"),
            Cliente = v.Cliente?.Nome ?? "Consumidor final",
            Vendedor = v.Usuario?.Nome ?? "-",
            Itens = v.Itens.Count,
            Total = v.Total.ToString("C", _ptBr),
            Status = StatusTexto(v.Status)
        }).ToList();
    }

    private static string StatusTexto(StatusVenda status) => status switch
    {
        StatusVenda.EmAberto => "Em aberto",
        StatusVenda.Finalizada => "Finalizada",
        StatusVenda.Cancelada => "Cancelada",
        _ => status.ToString()
    };

    private async void Filtrar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }
}
