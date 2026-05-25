using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class ClienteService
{
    private readonly AppDbContext _db;
    public ClienteService(AppDbContext db) => _db = db;

    public Task<List<Cliente>> ListarAsync(string? filtro = null)
    {
        var q = _db.Clientes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro))
            q = q.Where(c => c.Nome.Contains(filtro) ||
                            (c.CpfCnpj != null && c.CpfCnpj.Contains(filtro)));
        return q.OrderBy(c => c.Nome).Take(500).ToListAsync();
    }

    public Task<Cliente?> BuscarPorIdAsync(int id) =>
        _db.Clientes.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Result<Cliente>> SalvarAsync(Cliente cliente)
    {
        if (string.IsNullOrWhiteSpace(cliente.Nome))
            return Result.Falha<Cliente>("Nome é obrigatório.");

        if (cliente.Id == 0)
            _db.Clientes.Add(cliente);
        else
        {
            cliente.AtualizadoEm = DateTime.Now;
            _db.Clientes.Update(cliente);
        }
        await _db.SaveChangesAsync();
        return Result.Ok(cliente);
    }

    public async Task<Result> ExcluirAsync(int id)
    {
        var c = await _db.Clientes.FindAsync(id);
        if (c == null) return Result.Falha("Cliente não encontrado.");
        var temVenda = await _db.Vendas.AnyAsync(v => v.ClienteId == id);
        if (temVenda) { c.Ativo = false; c.AtualizadoEm = DateTime.Now; }
        else _db.Clientes.Remove(c);
        await _db.SaveChangesAsync();
        return Result.Ok();
    }
}
