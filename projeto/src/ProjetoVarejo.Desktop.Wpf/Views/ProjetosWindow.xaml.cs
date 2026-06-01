using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ProjetosWindow : UserControl
{
    private readonly AppDbContext _db;
    private readonly CultureInfo _ptBr = new("pt-BR");

    public ProjetosWindow(AppDbContext db)
    {
        _db = db;
        InitializeComponent();
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var clientes = await _db.Clientes.AsNoTracking().CountAsync(c => c.Ativo);
        var produtos = await _db.Produtos.AsNoTracking().CountAsync(p => p.Ativo);
        var fornecedores = await _db.Fornecedores.AsNoTracking().CountAsync(f => f.Ativo);

        var projetos = new List<ProjetoLinha>
        {
            new("Expansão do CRM", "Administrativo", "Comercial", DateTime.Today.AddDays(45), "Em execução", 65),
            new("Portal de Aquisição", "Suprimentos", "Compras", DateTime.Today.AddDays(60), "Em execução", 52),
            new("Orquestração de Produção", "Operações", "PCP", DateTime.Today.AddDays(90), "Planejamento", 30),
            new("Torre de Controle Logístico", "Suprimentos", "Logística", DateTime.Today.AddDays(75), "Em execução", 48),
            new("Dashboard Financeiro Executivo", "Financeiro", "Controladoria", DateTime.Today.AddDays(30), "Concluído", 100),
            new("Governança de Auditoria", "BI e Controle", "TI", DateTime.Today.AddDays(20), "Concluído", 100),
            new("Conector Ecommerce", "Digital", "Marketing", DateTime.Today.AddDays(80), "Planejamento", 25),
            new("Automação de Marketing", "Digital", "Marketing", DateTime.Today.AddDays(95), "Planejamento", 18),
            new($"Saneamento de cadastros ({clientes + produtos + fornecedores:N0} registros)", "Administrativo", "Backoffice", DateTime.Today.AddDays(50), "Em execução", 58)
        };

        var ativos = projetos.Count(p => p.Status != "Concluído");
        var concluidos = projetos.Count(p => p.Status == "Concluído");
        var progressoMedio = projetos.Average(p => p.Percentual);

        LblAtivos.Text = ativos.ToString("N0", _ptBr);
        LblConcluidos.Text = concluidos.ToString("N0", _ptBr);
        LblProgressoMedio.Text = progressoMedio.ToString("N0", _ptBr) + "%";

        DgProjetos.ItemsSource = projetos.Select(p => new
        {
            p.Nome,
            p.Area,
            p.Responsavel,
            Prazo = p.Prazo.ToString("dd/MM/yyyy"),
            p.Status,
            Progresso = p.Percentual.ToString("N0", _ptBr) + "%"
        }).ToList();
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }

    private sealed record ProjetoLinha(
        string Nome,
        string Area,
        string Responsavel,
        DateTime Prazo,
        string Status,
        decimal Percentual);
}
