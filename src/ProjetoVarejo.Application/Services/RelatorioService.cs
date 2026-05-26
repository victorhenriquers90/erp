using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Contracts.Services.DTOs;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class RelatorioService : IRelatorioService
{
    private readonly IUnitOfWork _unitOfWork;
    public RelatorioService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<List<VendaDiariaItem>> VendasPorDiaAsync(DateTime de, DateTime ate)
    {
        // EF Core 8 não traduz Select(new Record(...)) direto após GroupBy: usar tipo anônimo, materializar e converter
        var raw = await _unitOfWork.Vendas.Query()
            .Where(v => v.Status == StatusVenda.Finalizada && v.FinalizadaEm >= de && v.FinalizadaEm < ate)
            .GroupBy(v => v.FinalizadaEm!.Value.Date)
            .Select(g => new { Dia = g.Key, Qtd = g.Count(), Total = g.Sum(v => v.Total) })
            .OrderBy(x => x.Dia)
            .ToListAsync();
        return raw.Select(x => new VendaDiariaItem { Data = x.Dia, Quantidade = x.Qtd, Total = x.Total }).ToList();
    }

    public async Task<List<VendaPorFormaItem>> VendasPorFormaPagamentoAsync(DateTime de, DateTime ate)
    {
        var raw = await _unitOfWork.PagamentosVenda.Query()
            .Where(p => p.Venda.Status == StatusVenda.Finalizada
                     && p.Venda.FinalizadaEm >= de && p.Venda.FinalizadaEm < ate)
            .GroupBy(p => p.FormaPagamento)
            .Select(g => new { Forma = g.Key, Total = g.Sum(x => x.Valor), Qtd = g.Count() })
            .OrderByDescending(x => x.Total) // Move to database level
            .ToListAsync();
        return raw
            .Select(x => new VendaPorFormaItem { Forma = x.Forma.ToString(), Total = x.Total, Quantidade = x.Qtd, Percentual = 0 })
            .ToList();
    }

    public async Task<List<VendaPorVendedorItem>> VendasPorVendedorAsync(DateTime de, DateTime ate)
    {
        var raw = await _unitOfWork.Vendas.Query()
            .Where(v => v.Status == StatusVenda.Finalizada && v.FinalizadaEm >= de && v.FinalizadaEm < ate)
            .GroupBy(v => v.Usuario.Nome)
            .Select(g => new { Vendedor = g.Key, Qtd = g.Count(), Total = g.Sum(v => v.Total) })
            .OrderByDescending(x => x.Total) // Move to database level
            .ToListAsync();
        return raw
            .Select(x => new VendaPorVendedorItem {
                NomeVendedor = x.Vendedor,
                QuantidadeVendas = x.Qtd,
                TotalVendas = x.Total,
                TicketMedio = x.Qtd > 0 ? x.Total / x.Qtd : 0
            })
            .ToList();
    }

    public async Task<List<ProdutoRankingItem>> CurvaAbcAsync(DateTime de, DateTime ate)
    {
        var lista = await _unitOfWork.ItensVenda.Query()
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
            resultado.Add(new ProdutoRankingItem { Codigo = p.Codigo, Descricao = p.Descricao, QuantidadeVendida = p.Qtd, TotalVendido = p.Total, ClassificacaoAbc = classe });
        }
        return resultado;
    }

    public async Task<List<ProdutoRankingItem>> TopProdutosAsync(DateTime de, DateTime ate, int n = 20)
    {
        var raw = await _unitOfWork.ItensVenda.Query()
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
            .Select(x => new ProdutoRankingItem { Codigo = x.Codigo, Descricao = x.Descricao, QuantidadeVendida = x.Qtd, TotalVendido = x.Total })
            .ToList();
    }

    public async Task<List<FluxoCaixaItem>> FluxoCaixaAsync(DateTime de, DateTime ate)
    {
        var vendas = await _unitOfWork.Vendas.Query()
            .Where(v => v.Status == StatusVenda.Finalizada && v.FinalizadaEm >= de && v.FinalizadaEm < ate)
            .GroupBy(v => v.FinalizadaEm!.Value.Date)
            .Select(g => new { Dia = g.Key, Total = g.Sum(v => v.Total) })
            .ToListAsync();

        // TODO: ContasFinanceiras needs proper implementation - currently returns empty list
        // This should query ContasFinanceiras and group by payment date
        var contasPagasDays = new List<DateTime>();

        var dias = vendas.Select(v => v.Dia)
            .Union(contasPagasDays)
            .Distinct().OrderBy(d => d).ToList();

        var resultado = new List<FluxoCaixaItem>();
        foreach (var dia in dias)
        {
            var entradaVenda = vendas.FirstOrDefault(v => v.Dia == dia)?.Total ?? 0;
            // TODO: Implement ContasFinanceiras properly to get entrada from accounts receivable
            var entradaConta = 0m;
            // TODO: Implement ContasFinanceiras properly to get saida from accounts payable
            var saida = 0m;
            var entradas = entradaVenda + entradaConta;
            resultado.Add(new FluxoCaixaItem { Data = dia, Entradas = entradas, Saidas = saida, SaldoAcumulado = entradas - saida });
        }
        return resultado;
    }
}
