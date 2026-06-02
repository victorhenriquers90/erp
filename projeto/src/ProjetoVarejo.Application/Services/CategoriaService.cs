using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;

namespace ProjetoVarejo.Application.Services;

public class CategoriaService
{
    private readonly AppDbContext _db;
    public CategoriaService(AppDbContext db) => _db = db;

    public Task<List<Categoria>> ListarAsync() =>
        _db.Categorias.Where(c => c.Ativo).OrderBy(c => c.Nome).ToListAsync();
}
