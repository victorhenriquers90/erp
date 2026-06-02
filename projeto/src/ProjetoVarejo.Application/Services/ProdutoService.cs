using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class ProdutoService
{
    private readonly AppDbContext _db;

    public ProdutoService(AppDbContext db) => _db = db;

    public Task<List<Produto>> ListarAsync(string? filtro = null)
    {
        var q = _db.Produtos.Include(p => p.Categoria).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            q = q.Where(p => p.Descricao.Contains(filtro)
                          || p.Codigo.Contains(filtro)
                          || (p.CodigoBarras != null && p.CodigoBarras.Contains(filtro)));
        }
        return q.OrderBy(p => p.Descricao).Take(500).ToListAsync();
    }

    public Task<Produto?> BuscarPorCodigoAsync(string codigo) =>
        _db.Produtos.FirstOrDefaultAsync(p =>
            p.Codigo == codigo || p.CodigoBarras == codigo);

    public Task<Produto?> BuscarPorIdAsync(int id) =>
        _db.Produtos.Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Result<Produto>> SalvarAsync(Produto produto)
    {
        if (string.IsNullOrWhiteSpace(produto.Codigo))
            return Result.Falha<Produto>("Código é obrigatório.");
        if (string.IsNullOrWhiteSpace(produto.Descricao))
            return Result.Falha<Produto>("Descrição é obrigatória.");
        if (produto.PrecoVenda <= 0)
            return Result.Falha<Produto>("Preço de venda deve ser maior que zero.");

        var duplicado = await _db.Produtos.AnyAsync(p =>
            p.Codigo == produto.Codigo && p.Id != produto.Id);
        if (duplicado)
            return Result.Falha<Produto>("Já existe um produto com este código.");

        if (produto.Id == 0)
        {
            _db.Produtos.Add(produto);
        }
        else
        {
            produto.AtualizadoEm = DateTime.Now;
            _db.Produtos.Update(produto);
        }
        await _db.SaveChangesAsync();
        return Result.Ok(produto);
    }

    public async Task<Result> ExcluirAsync(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        if (produto == null) return Result.Falha("Produto não encontrado.");
        var temVendas = await _db.ItensVenda.AnyAsync(i => i.ProdutoId == id);
        if (temVendas)
        {
            produto.Ativo = false;
            produto.AtualizadoEm = DateTime.Now;
        }
        else
        {
            _db.Produtos.Remove(produto);
        }
        await _db.SaveChangesAsync();
        return Result.Ok();
    }
}
