using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class FornecedorService
{
    private readonly AppDbContext _db;
    public FornecedorService(AppDbContext db) => _db = db;

    public Task<List<Fornecedor>> ListarAsync(string? filtro = null)
    {
        var q = _db.Fornecedores.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro))
            q = q.Where(f => f.RazaoSocial.Contains(filtro) ||
                            (f.NomeFantasia != null && f.NomeFantasia.Contains(filtro)) ||
                             f.Cnpj.Contains(filtro));
        return q.OrderBy(f => f.RazaoSocial).Take(500).ToListAsync();
    }

    public Task<Fornecedor?> BuscarPorIdAsync(int id) =>
        _db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id);

    public async Task<Result<Fornecedor>> SalvarAsync(Fornecedor f)
    {
        if (string.IsNullOrWhiteSpace(f.RazaoSocial))
            return Result.Falha<Fornecedor>("Razão Social é obrigatória.");
        if (string.IsNullOrWhiteSpace(f.Cnpj))
            return Result.Falha<Fornecedor>("CNPJ é obrigatório.");

        var duplicado = await _db.Fornecedores.AnyAsync(x => x.Cnpj == f.Cnpj && x.Id != f.Id);
        if (duplicado) return Result.Falha<Fornecedor>("CNPJ já cadastrado.");

        if (f.Id == 0) _db.Fornecedores.Add(f);
        else { f.AtualizadoEm = DateTime.Now; _db.Fornecedores.Update(f); }
        await _db.SaveChangesAsync();
        return Result.Ok(f);
    }

    public async Task<Result> ExcluirAsync(int id)
    {
        var f = await _db.Fornecedores.FindAsync(id);
        if (f == null) return Result.Falha("Fornecedor não encontrado.");
        f.Ativo = false;
        f.AtualizadoEm = DateTime.Now;
        await _db.SaveChangesAsync();
        return Result.Ok();
    }
}
