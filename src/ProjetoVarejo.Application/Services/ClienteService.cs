using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Microsoft.EntityFrameworkCore;

namespace ProjetoVarejo.Application.Services;

public class ClienteService
{
    private readonly IUnitOfWork _unitOfWork;
    public ClienteService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public Task<List<Cliente>> ListarAsync(string? filtro = null)
    {
        var q = _unitOfWork.Clientes.Query().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro))
            q = q.Where(c => c.Nome.Contains(filtro) ||
                            (c.CpfCnpj != null && c.CpfCnpj.Contains(filtro)));
        return q.OrderBy(c => c.Nome).Take(500).ToListAsync();
    }

    public Task<Cliente?> BuscarPorIdAsync(int id) =>
        _unitOfWork.Clientes.Query().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Result<Cliente>> SalvarAsync(Cliente cliente)
    {
        if (string.IsNullOrWhiteSpace(cliente.Nome))
            return Result.Falha<Cliente>("Nome é obrigatório.");

        if (cliente.Id == 0)
            await _unitOfWork.Clientes.InsertAsync(cliente);
        else
        {
            cliente.AtualizadoEm = DateTime.Now;
            await _unitOfWork.Clientes.UpdateAsync(cliente);
        }
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok(cliente);
    }

    public async Task<Result> ExcluirAsync(int id)
    {
        var c = await _unitOfWork.Clientes.GetByIdAsync(id);
        if (c == null) return Result.Falha("Cliente não encontrado.");
        var temVenda = await _unitOfWork.Vendas.Query().AnyAsync(v => v.ClienteId == id);
        if (temVenda) { c.Ativo = false; c.AtualizadoEm = DateTime.Now; await _unitOfWork.Clientes.UpdateAsync(c); }
        else await _unitOfWork.Clientes.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok();
    }
}
