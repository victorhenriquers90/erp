using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class RelatorioService
{
    private readonly AppDbContext _db;
    public RelatorioService(AppDbContext db) => _db = db;

    public record VendaDiariaItem(DateTime Dia, int Quantidade, decimal Total);
    public record VendaPorFormaItem(FormaPagamentoTipo Forma, decimal Total, int Qtd);
    public record VendaPorVendedorItem(string Vendedor, int QtdVendas, decimal Total, decimal TicketMedio);
    public record ProdutoRankingItem(string Codigo, string Descricao, decimal Quantidade, decimal Faturamento, string Classe);
    public record FluxoCaixaItem(DateTime Dia, decimal Entradas, decimal Saidas, decimal Saldo);

    public async Task<List<VendaDiariaItem>> VendasPorDiaAsync(DateTime de, DateTime ate)
    {
        // EF Core 8 não traduz Select(new Record(...)) direto após GroupBy: usar tipo anônimo, materializar e converter
        var raw = await _db.Vendas
            .Where(v => v.Status == StatusVenda.Finalizada && v.FinalizadaEm >= de && v.FinalizadaEm < ate)
            .GroupBy(v => v.FinalizadaEm!.Value.Date)
            .Select(g => new { Dia = g.Key, Qtd = g.Count(), Total = g.Sum(v => v.Total) })
            .OrderBy(x => x.Dia)
            .ToListAsync();
        return raw.Select(x => new VendaDiariaItem(x.Dia, x.Qtd, x.Total)).ToList();
    }

    public async Task<List<VendaPorFormaItem>> VendasPorFormaPagamentoAsync(DateTime de, DateTime ate)
    {
        var raw = await _db.PagamentosVenda
            .Where(p => p.Venda.Status == StatusVenda.Finalizada
                     && p.Venda.FinalizadaEm >= de && p.Venda.FinalizadaEm < ate)
            .GroupBy(p => p.FormaPagamento)
            .Select(g => new { Forma = g.Key, Total = g.Sum(x => x.Valor), Qtd = g.Count() })
            .ToListAsync();
        return raw
            .Select(x => new VendaPorFormaItem(x.Forma, x.Total, x.Qtd))
            .OrderByDescending(x => x.Total)
            .ToList();
    }

    public async Task<List<VendaPorVendedorItem>> VendasPorVendedorAsync(DateTime de, DateTime ate)
    {
        var raw = await _db.Vendas
            .Where(v => v.Status == StatusVenda.Finalizada && v.FinalizadaEm >= de && v.FinalizadaEm < ate)
            .GroupBy(v => v.Usuario.Nome)
            .Select(g => new { Vendedor = g.Key, Qtd = g.Count(), Total = g.Sum(v => v.Total) })
            .ToListAsync();
        return raw
            .Select(x => new VendaPorVendedorItem(x.Vendedor, x.Qtd, x.Total, x.Qtd > 0 ? x.Total / x.Qtd : 0))
            .OrderByDescending(x => x.Total)
            .ToList();
    }

    public async Task<List<ProdutoRankingItem>> CurvaAbcAsync(DateTime de, DateTime ate)
    {
        var lista = await _db.ItensVenda
            .Where(i => i.Venda.Status == StatusVenda.Finalizada
                     && i.Venda.FinalizadaEm >= de && i.Venda.FinalizadaEm < ate)
            .GroupBy(i => new { i.ProdutoId, i.Produto.Codigo, i.Produto.Descricao })
            .Select(g => new
            {
                g.Key.Codigo,
                g.Key.Descricao,
                Qtd = g.Sum(x => x.Quantidade),
                Total = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.Total)
            .ToListAsync();

        var totalGeral = lista.Sum(x => x.Total);
        var acumulado = 0m;
        var resultado = new List<ProdutoRankingItem>();
        foreach (var p in lista)
        {
            acumulado += p.Total;
            var perc = totalGeral == 0 ? 0 : acumulado / totalGeral;
            var classe = perc <= 0.8m ? "A" : perc <= 0.95m ? "B" : "C";
            resultado.Add(new ProdutoRankingItem(p.Codigo, p.Descricao, p.Qtd, p.Total, classe));
        }
        return resultado;
    }

    public async Task<List<ProdutoRankingItem>> TopProdutosAsync(DateTime de, DateTime ate, int n = 20)
    {
        var raw = await _db.ItensVenda
            .Where(i => i.Venda.Status == StatusVenda.Finalizada
                     && i.Venda.FinalizadaEm >= de && i.Venda.FinalizadaEm < ate)
            .GroupBy(i => new { i.Produto.Codigo, i.Produto.Descricao })
            .Select(g => new
            {
                g.Key.Codigo,
                g.Key.Descricao,
                Qtd = g.Sum(x => x.Quantidade),
                Total = g.Sum(x => x.Total)
            })
            .OrderByDescending(x => x.Total)
            .Take(n)
            .ToListAsync();
        return raw
            .Select(x => new ProdutoRankingItem(x.Codigo, x.Descricao, x.Qtd, x.Total, ""))
            .ToList();
    }

    public async Task<List<FluxoCaixaItem>> FluxoCaixaAsync(DateTime de, DateTime ate)
    {
        var vendas = await _db.Vendas
            .Where(v => v.Status == StatusVenda.Finalizada && v.FinalizadaEm >= de && v.FinalizadaEm < ate)
            .GroupBy(v => v.FinalizadaEm!.Value.Date)
            .Select(g => new { Dia = g.Key, Total = g.Sum(v => v.Total) })
            .ToListAsync();

        var contasPagas = await _db.ContasFinanceiras
            .Where(c => c.Status == StatusConta.Paga
                     && c.DataPagamento >= de && c.DataPagamento < ate)
            .GroupBy(c => new { Dia = c.DataPagamento!.Value.Date, c.Tipo })
            .Select(g => new { g.Key.Dia, g.Key.Tipo, Total = g.Sum(x => x.ValorPago) })
            .ToListAsync();

        var dias = vendas.Select(v => v.Dia)
            .Union(contasPagas.Select(c => c.Dia))
            .Distinct().OrderBy(d => d).ToList();

        var resultado = new List<FluxoCaixaItem>();
        foreach (var dia in dias)
        {
            var entradaVenda = vendas.FirstOrDefault(v => v.Dia == dia)?.Total ?? 0;
            var entradaConta = contasPagas.Where(c => c.Dia == dia && c.Tipo == TipoConta.Receber).Sum(c => c.Total);
            var saida = contasPagas.Where(c => c.Dia == dia && c.Tipo == TipoConta.Pagar).Sum(c => c.Total);
            var entradas = entradaVenda + entradaConta;
            resultado.Add(new FluxoCaixaItem(dia, entradas, saida, entradas - saida));
        }
        return resultado;
    }
}
