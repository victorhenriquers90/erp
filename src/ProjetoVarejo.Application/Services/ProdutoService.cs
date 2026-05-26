using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Microsoft.EntityFrameworkCore;

namespace ProjetoVarejo.Application.Services;

public class ProdutoService : IProdutoService
{
    private readonly IUnitOfWork _unitOfWork;

    public ProdutoService(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public Task<List<Produto>> ListarAsync(string? filtro = null)
    {
        var q = _unitOfWork.Produtos.Query().Include(p => p.Categoria).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro))
        {
            q = q.Where(p => p.Descricao.Contains(filtro)
                          || p.Codigo.Contains(filtro)
                          || (p.CodigoBarras != null && p.CodigoBarras.Contains(filtro)));
        }
        return q.OrderBy(p => p.Descricao).Take(500).ToListAsync();
    }

    public Task<List<Produto>> ListarParaVendaAsync(string? filtro = null)
    {
        var q = _unitOfWork.Produtos.Query()
            .Include(p => p.Categoria)
            .Where(p => p.Ativo)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro))
        {
            q = q.Where(p => p.Descricao.Contains(filtro)
                          || p.Codigo.Contains(filtro)
                          || (p.CodigoBarras != null && p.CodigoBarras.Contains(filtro)));
        }

        return q.OrderBy(p => p.Descricao).Take(200).ToListAsync();
    }

    public Task<Produto?> BuscarPorCodigoAsync(string codigo) =>
        _unitOfWork.Produtos.Query().FirstOrDefaultAsync(p => p.Ativo &&
            (p.Codigo == codigo || p.CodigoBarras == codigo));

    public Task<Produto?> BuscarPorIdAsync(int id) =>
        _unitOfWork.Produtos.Query().Include(p => p.Categoria).FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Result<Produto>> SalvarAsync(Produto produto)
    {
        if (string.IsNullOrWhiteSpace(produto.Codigo))
            return Result.Falha<Produto>("Código é obrigatório.");
        if (string.IsNullOrWhiteSpace(produto.Descricao))
            return Result.Falha<Produto>("Descrição é obrigatória.");
        if (produto.PrecoVenda <= 0)
            return Result.Falha<Produto>("Preço de venda deve ser maior que zero.");

        var duplicado = await _unitOfWork.Produtos.Query().AnyAsync(p =>
            p.Codigo == produto.Codigo && p.Id != produto.Id);
        if (duplicado)
            return Result.Falha<Produto>("Já existe um produto com este código.");

        if (produto.Id == 0)
        {
            await _unitOfWork.Produtos.InsertAsync(produto);
        }
        else
        {
            produto.AtualizadoEm = DateTime.Now;
            await _unitOfWork.Produtos.UpdateAsync(produto);
        }
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok(produto);
    }

    public async Task<Result> ExcluirAsync(int id)
    {
        var produto = await _unitOfWork.Produtos.GetByIdAsync(id);
        if (produto == null) return Result.Falha("Produto não encontrado.");
        var temVendas = await _unitOfWork.ItensVenda.Query().AnyAsync(i => i.ProdutoId == id);
        if (temVendas)
        {
            produto.Ativo = false;
            produto.AtualizadoEm = DateTime.Now;
            await _unitOfWork.Produtos.UpdateAsync(produto);
        }
        else
        {
            await _unitOfWork.Produtos.DeleteAsync(id);
        }
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok();
    }
}
