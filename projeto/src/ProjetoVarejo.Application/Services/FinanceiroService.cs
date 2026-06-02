using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class FinanceiroService
{
    private readonly AppDbContext _db;
    public FinanceiroService(AppDbContext db) => _db = db;

    public Task<List<ContaFinanceira>> ListarAsync(TipoConta? tipo = null, StatusConta? status = null, DateTime? de = null, DateTime? ate = null)
    {
        var q = _db.ContasFinanceiras
            .Include(c => c.Cliente)
            .Include(c => c.Fornecedor)
            .AsQueryable();
        if (tipo.HasValue) q = q.Where(c => c.Tipo == tipo.Value);
        if (status.HasValue) q = q.Where(c => c.Status == status.Value);
        if (de.HasValue) q = q.Where(c => c.DataVencimento >= de.Value);
        if (ate.HasValue) q = q.Where(c => c.DataVencimento <= ate.Value);
        return q.OrderBy(c => c.DataVencimento).Take(500).ToListAsync();
    }

    public async Task<Result<ContaFinanceira>> SalvarAsync(ContaFinanceira c)
    {
        if (string.IsNullOrWhiteSpace(c.Descricao))
            return Result.Falha<ContaFinanceira>("Descrição é obrigatória.");
        if (c.Valor <= 0)
            return Result.Falha<ContaFinanceira>("Valor deve ser maior que zero.");
        if (c.Id == 0) _db.ContasFinanceiras.Add(c);
        else { c.AtualizadoEm = DateTime.Now; _db.ContasFinanceiras.Update(c); }
        await _db.SaveChangesAsync();
        return Result.Ok(c);
    }

    public async Task<Result> QuitarAsync(int contaId, DateTime dataPagamento, decimal valorPago, FormaPagamentoTipo forma, decimal juros = 0, decimal multa = 0, decimal desconto = 0)
    {
        var c = await _db.ContasFinanceiras.FindAsync(contaId);
        if (c == null) return Result.Falha("Conta não encontrada.");
        if (c.Status == StatusConta.Paga) return Result.Falha("Conta já está paga.");

        c.DataPagamento = dataPagamento;
        c.ValorPago = valorPago;
        c.Juros = juros;
        c.Multa = multa;
        c.Desconto = desconto;
        c.FormaPagamento = forma;
        c.Status = StatusConta.Paga;
        c.AtualizadoEm = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<(decimal totalReceber, decimal totalPagar, decimal saldoPrevisto)> ResumoAsync(DateTime de, DateTime ate)
    {
        var contas = await _db.ContasFinanceiras
            .Where(c => c.Status == StatusConta.EmAberto || c.Status == StatusConta.Atrasada)
            .Where(c => c.DataVencimento >= de && c.DataVencimento <= ate)
            .ToListAsync();

        var receber = contas.Where(c => c.Tipo == TipoConta.Receber).Sum(c => c.Valor);
        var pagar = contas.Where(c => c.Tipo == TipoConta.Pagar).Sum(c => c.Valor);
        return (receber, pagar, receber - pagar);
    }

    public async Task<decimal> TotalVendasDoDiaAsync(DateTime data)
    {
        var inicio = data.Date;
        var fim = inicio.AddDays(1);
        return await _db.Vendas
            .Where(v => v.Status == StatusVenda.Finalizada && v.FinalizadaEm >= inicio && v.FinalizadaEm < fim)
            .SumAsync(v => (decimal?)v.Total) ?? 0;
    }
}
