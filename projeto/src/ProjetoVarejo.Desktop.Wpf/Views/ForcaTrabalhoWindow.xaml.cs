using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ForcaTrabalhoWindow : UserControl
{
    private readonly AppDbContext _db;
    private readonly CultureInfo _ptBr = new("pt-BR");

    public ForcaTrabalhoWindow(AppDbContext db)
    {
        _db = db;
        InitializeComponent();
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var inicio = DateTime.Today.AddDays(-30);

        var usuarios = await _db.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.Nome)
            .ToListAsync();

        var vendasPorUsuario = await _db.Vendas
            .AsNoTracking()
            .Where(v => v.DataVenda >= inicio && v.Status == StatusVenda.Finalizada)
            .GroupBy(v => v.UsuarioId)
            .Select(g => new
            {
                UsuarioId = g.Key,
                Quantidade = g.Count(),
                Total = g.Sum(x => x.Total)
            })
            .ToDictionaryAsync(x => x.UsuarioId);

        var ativos = usuarios.Count(u => u.Ativo);
        var gestores = usuarios.Count(u => u.Ativo && (u.Perfil == PerfilUsuario.Administrador || u.Perfil == PerfilUsuario.Gerente));
        var vendas30 = vendasPorUsuario.Values.Sum(v => v.Quantidade);
        var faturamento30 = vendasPorUsuario.Values.Sum(v => v.Total);

        LblAtivos.Text = ativos.ToString("N0", _ptBr);
        LblGestores.Text = gestores.ToString("N0", _ptBr);
        LblVendas30.Text = vendas30.ToString("N0", _ptBr);
        LblFaturamento30.Text = faturamento30.ToString("C", _ptBr);

        DgEquipe.ItemsSource = usuarios.Select(u =>
        {
            var temVenda = vendasPorUsuario.TryGetValue(u.Id, out var kpi);
            return new
            {
                u.Nome,
                u.Login,
                Perfil = PerfilTexto(u.Perfil),
                UltimoAcesso = u.UltimoAcesso?.ToString("dd/MM/yyyy HH:mm") ?? "-",
                Vendas = temVenda ? kpi!.Quantidade.ToString("N0", _ptBr) : "0",
                Faturamento = temVenda ? kpi!.Total.ToString("C", _ptBr) : 0m.ToString("C", _ptBr),
                Status = u.Ativo ? "Ativo" : "Inativo"
            };
        }).ToList();
    }

    private static string PerfilTexto(PerfilUsuario perfil) => perfil switch
    {
        PerfilUsuario.Administrador => "Administrador",
        PerfilUsuario.Gerente => "Gerente",
        PerfilUsuario.Caixa => "Caixa",
        PerfilUsuario.Estoquista => "Estoquista",
        _ => perfil.ToString()
    };

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }
}
