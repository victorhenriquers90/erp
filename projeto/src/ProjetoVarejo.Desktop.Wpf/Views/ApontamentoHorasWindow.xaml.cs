using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ApontamentoHorasWindow : UserControl
{
    private readonly AppDbContext _db;
    private readonly CultureInfo _ptBr = new("pt-BR");

    public ApontamentoHorasWindow(AppDbContext db)
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

        var sessoes = await _db.CaixasSessao
            .AsNoTracking()
            .Include(c => c.UsuarioAbertura)
            .Where(c => c.AbertaEm >= de && c.AbertaEm < ate && c.FechadaEm != null)
            .ToListAsync();

        var linhas = sessoes
            .GroupBy(c => new
            {
                c.UsuarioAberturaId,
                Nome = c.UsuarioAbertura.Nome,
                Perfil = c.UsuarioAbertura.Perfil
            })
            .Select(g =>
            {
                var horas = g.Sum(s => (s.FechadaEm!.Value - s.AbertaEm).TotalHours);
                var sessoesTotal = g.Count();
                var ultimo = g.Max(s => s.FechadaEm);
                return new
                {
                    Colaborador = g.Key.Nome,
                    Perfil = PerfilTexto(g.Key.Perfil),
                    Sessoes = sessoesTotal.ToString("N0", _ptBr),
                    HorasNumero = horas,
                    Horas = horas.ToString("N1", _ptBr) + " h",
                    MediaSessao = (sessoesTotal == 0 ? 0 : horas / sessoesTotal).ToString("N1", _ptBr) + " h",
                    UltimoFechamento = ultimo?.ToString("dd/MM/yyyy HH:mm") ?? "-"
                };
            })
            .OrderByDescending(x => x.HorasNumero)
            .ToList();

        var horasTotais = linhas.Sum(x => x.HorasNumero);
        var totalSessoes = sessoes.Count;
        var mediaColaborador = linhas.Count == 0 ? 0 : horasTotais / linhas.Count;

        LblHorasTotais.Text = horasTotais.ToString("N1", _ptBr) + " h";
        LblSessoes.Text = totalSessoes.ToString("N0", _ptBr);
        LblMedia.Text = mediaColaborador.ToString("N1", _ptBr) + " h";

        DgHoras.ItemsSource = linhas.Select(x => new
        {
            x.Colaborador,
            x.Perfil,
            x.Sessoes,
            x.Horas,
            x.MediaSessao,
            x.UltimoFechamento
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

    private async void Filtrar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }
}
