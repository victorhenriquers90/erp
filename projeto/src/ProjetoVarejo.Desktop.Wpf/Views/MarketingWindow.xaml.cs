using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class MarketingWindow : UserControl
{
    private readonly AppDbContext _db;
    private readonly CultureInfo _ptBr = new("pt-BR");

    public MarketingWindow(AppDbContext db)
    {
        _db = db;
        InitializeComponent();
        DtDe.SelectedDate = DateTime.Today.AddDays(-90);
        DtAte.SelectedDate = DateTime.Today;
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var de = DtDe.SelectedDate ?? DateTime.Today.AddDays(-90);
        var ate = (DtAte.SelectedDate ?? DateTime.Today).AddDays(1);
        var corteRecente = DateTime.Today.AddDays(-30);
        var corteReativacao = DateTime.Today.AddDays(-60);

        var vendas = await _db.Vendas
            .AsNoTracking()
            .Include(v => v.Cliente)
            .Where(v => v.DataVenda >= de && v.DataVenda < ate && v.Status == StatusVenda.Finalizada && v.ClienteId != null)
            .ToListAsync();

        var porCliente = vendas
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente!.Nome })
            .Select(g => new
            {
                ClienteId = g.Key.ClienteId!.Value,
                Cliente = g.Key.Nome,
                Compras = g.Count(),
                Faturamento = g.Sum(x => x.Total),
                UltimaCompra = g.Max(x => x.DataVenda)
            })
            .ToList();

        var clientesAtivos = porCliente.Count(c => c.UltimaCompra >= corteRecente);
        var recorrentes = porCliente.Count(c => c.Compras >= 3);
        var reativacao = porCliente.Count(c => c.UltimaCompra < corteReativacao);
        var ticket = vendas.Count == 0 ? 0 : vendas.Average(v => v.Total);

        LblClientesAtivos.Text = clientesAtivos.ToString("N0", _ptBr);
        LblRecorrentes.Text = recorrentes.ToString("N0", _ptBr);
        LblReativacao.Text = reativacao.ToString("N0", _ptBr);
        LblTicket.Text = ticket.ToString("C", _ptBr);

        var segmentoVipClientes = porCliente.OrderByDescending(c => c.Faturamento).Take(50).Count();
        var segmentoFrequentes = porCliente.Count(c => c.Compras >= 5);
        var segmentoNovos = porCliente.Count(c => c.Compras == 1 && c.UltimaCompra >= corteRecente);
        var segmentoDormindo = porCliente.Count(c => c.UltimaCompra < corteReativacao);

        DgSegmentos.ItemsSource = new[]
        {
            new
            {
                Segmento = "VIP (maior faturamento)",
                Clientes = segmentoVipClientes.ToString("N0", _ptBr),
                Potencial = porCliente.OrderByDescending(c => c.Faturamento).Take(50).Sum(c => c.Faturamento).ToString("C", _ptBr),
                Acao = "Campanha premium com benefício exclusivo e atendimento dedicado."
            },
            new
            {
                Segmento = "Frequentes",
                Clientes = segmentoFrequentes.ToString("N0", _ptBr),
                Potencial = porCliente.Where(c => c.Compras >= 5).Sum(c => c.Faturamento).ToString("C", _ptBr),
                Acao = "Programa de fidelidade com bônus por recorrência."
            },
            new
            {
                Segmento = "Novos clientes",
                Clientes = segmentoNovos.ToString("N0", _ptBr),
                Potencial = porCliente.Where(c => c.Compras == 1 && c.UltimaCompra >= corteRecente).Sum(c => c.Faturamento).ToString("C", _ptBr),
                Acao = "Fluxo de onboarding com segunda compra incentivada."
            },
            new
            {
                Segmento = "Dormindo / reativação",
                Clientes = segmentoDormindo.ToString("N0", _ptBr),
                Potencial = porCliente.Where(c => c.UltimaCompra < corteReativacao).Sum(c => c.Faturamento).ToString("C", _ptBr),
                Acao = "Campanha de reativação com cupom de retorno."
            }
        };

        DgTopClientes.ItemsSource = porCliente
            .OrderByDescending(c => c.Faturamento)
            .Take(200)
            .Select(c => new
            {
                c.Cliente,
                Compras = c.Compras.ToString("N0", _ptBr),
                Faturamento = c.Faturamento.ToString("C", _ptBr),
                UltimaCompra = c.UltimaCompra.ToString("dd/MM/yyyy HH:mm")
            })
            .ToList();
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }
}
