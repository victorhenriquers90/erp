using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class CadeiaSuprimentosWindow : UserControl
{
    private readonly AppDbContext _db;
    private readonly CultureInfo _ptBr = new("pt-BR");

    public CadeiaSuprimentosWindow(AppDbContext db)
    {
        _db = db;
        InitializeComponent();
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var inicio = DateTime.Today.AddDays(-30);

        var fornecedoresAtivos = await _db.Fornecedores.AsNoTracking().CountAsync(f => f.Ativo);

        var movimentos = await _db.MovimentosEstoque
            .AsNoTracking()
            .Include(m => m.Produto)
            .ThenInclude(p => p.Categoria)
            .Include(m => m.Fornecedor)
            .Where(m => m.CriadoEm >= inicio)
            .ToListAsync();

        var entradasTipos = new[] { TipoMovimentoEstoque.Entrada, TipoMovimentoEstoque.AjusteEntrada, TipoMovimentoEstoque.Devolucao };
        var saidasTipos = new[] { TipoMovimentoEstoque.Saida, TipoMovimentoEstoque.AjusteSaida };

        var entradas30 = movimentos.Where(m => entradasTipos.Contains(m.Tipo)).Sum(m => m.Quantidade);
        var saidas30 = movimentos.Where(m => saidasTipos.Contains(m.Tipo)).Sum(m => m.Quantidade);

        var produtos = await _db.Produtos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.Ativo && p.ControlaEstoque)
            .ToListAsync();

        var criticos = produtos.Count(p => p.Estoque <= p.EstoqueMinimo);

        var ultimasEntradasPorProduto = movimentos
            .Where(m => entradasTipos.Contains(m.Tipo))
            .GroupBy(m => m.ProdutoId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.CriadoEm).First());

        LblFornecedores.Text = fornecedoresAtivos.ToString("N0", _ptBr);
        LblCriticos.Text = criticos.ToString("N0", _ptBr);
        LblEntradas.Text = entradas30.ToString("N3", _ptBr);
        LblSaidas.Text = saidas30.ToString("N3", _ptBr);

        DgSuprimentos.ItemsSource = produtos
            .OrderBy(p => p.Estoque <= p.EstoqueMinimo ? 0 : 1)
            .ThenBy(p => p.Descricao)
            .Take(400)
            .Select(p =>
            {
                var temEntrada = ultimasEntradasPorProduto.TryGetValue(p.Id, out var ult);
                return new
                {
                    Produto = p.Descricao,
                    Categoria = p.Categoria?.Nome ?? "-",
                    Estoque = p.Estoque.ToString("N3", _ptBr),
                    Minimo = p.EstoqueMinimo.ToString("N3", _ptBr),
                    Cobertura = p.Estoque <= p.EstoqueMinimo ? "Repor" : "OK",
                    Fornecedor = temEntrada ? (ult!.Fornecedor?.NomeFantasia ?? ult!.Fornecedor?.RazaoSocial ?? "-") : "-",
                    UltimaEntrada = temEntrada ? ult!.CriadoEm.ToString("dd/MM/yyyy HH:mm") : "-"
                };
            })
            .ToList();
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }
}
