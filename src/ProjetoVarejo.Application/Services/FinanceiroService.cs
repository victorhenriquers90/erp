using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Microsoft.EntityFrameworkCore;

namespace ProjetoVarejo.Application.Services;

public class FinanceiroService : IFinanceiroService
{
    private readonly IUnitOfWork _unitOfWork;
    public FinanceiroService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    // NOTE: ContasFinanceiras appears to be a combined view/table for both ContasPagar and ContasReceber
    // This service would need refactoring to work properly with the split repositories
    // For now, accessing through separate repositories (ContasPagar/ContasReceber)

    public async Task<List<ContaFinanceira>> ListarAsync(TipoConta? tipo = null, StatusConta? status = null, DateTime? de = null, DateTime? ate = null)
    {
        var q = _unitOfWork.ContasFinanceiras.Query();
        if (tipo.HasValue) q = q.Where(c => c.Tipo == tipo.Value);
        if (status.HasValue) q = q.Where(c => c.Status == status.Value);
        if (de.HasValue) q = q.Where(c => c.DataVencimento >= de.Value);
        if (ate.HasValue) q = q.Where(c => c.DataVencimento <= ate.Value);
        return await q.OrderBy(c => c.DataVencimento).Take(500).ToListAsync();
    }

    public async Task<Result<ContaFinanceira>> SalvarAsync(ContaFinanceira c)
    {
        if (string.IsNullOrWhiteSpace(c.Descricao))
            return Result.Falha<ContaFinanceira>("Descrição é obrigatória.");
        if (c.Valor <= 0)
            return Result.Falha<ContaFinanceira>("Valor deve ser maior que zero.");
        if (c.DataVencimento == default)
            return Result.Falha<ContaFinanceira>("Data de vencimento é obrigatória.");

        if (c.Id == 0)
            await _unitOfWork.ContasFinanceiras.InsertAsync(c);
        else
            await _unitOfWork.ContasFinanceiras.UpdateAsync(c);

        await _unitOfWork.SaveChangesAsync();
        return Result.Ok(c);
    }

    public async Task<Result> QuitarAsync(int contaId, DateTime dataPagamento, decimal valorPago, FormaPagamentoTipo forma, decimal juros = 0, decimal multa = 0, decimal desconto = 0)
    {
        var conta = await _unitOfWork.ContasFinanceiras.GetByIdAsync(contaId);
        if (conta == null)
            return Result.Falha($"Conta financeira {contaId} não encontrada.");
        if (conta.Status == StatusConta.Paga)
            return Result.Falha("Conta já está quitada.");

        conta.DataPagamento = dataPagamento;
        conta.ValorPago = valorPago;
        conta.FormaPagamento = forma;
        conta.Juros = juros;
        conta.Multa = multa;
        conta.Desconto = desconto;
        conta.Status = StatusConta.Paga;

        await _unitOfWork.ContasFinanceiras.UpdateAsync(conta);
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<(decimal totalReceber, decimal totalPagar, decimal saldoPrevisto)> ResumoAsync(DateTime de, DateTime ate)
    {
        var q = _unitOfWork.ContasFinanceiras.Query()
            .Where(c => (c.Status == StatusConta.EmAberto || c.Status == StatusConta.Atrasada)
                    && c.DataVencimento >= de && c.DataVencimento <= ate);

        var receber = await q.Where(c => c.Tipo == TipoConta.Receber).SumAsync(c => (decimal?)c.Valor) ?? 0;
        var pagar = await q.Where(c => c.Tipo == TipoConta.Pagar).SumAsync(c => (decimal?)c.Valor) ?? 0;
        return (receber, pagar, receber - pagar);
    }

    public async Task<decimal> TotalVendasDoDiaAsync(DateTime data)
    {
        var inicio = data.Date;
        var fim = inicio.AddDays(1);
        return await _unitOfWork.Vendas.Query()
            .Where(v => v.Status == StatusVenda.Finalizada && v.FinalizadaEm >= inicio && v.FinalizadaEm < fim)
            .SumAsync(v => (decimal?)v.Total) ?? 0;
    }
}
