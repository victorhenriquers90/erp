using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Infrastructure.Data;

namespace ProjetoVarejo.Infrastructure.Repositories;

/// <summary>
/// Implementação genérica do padrão Repository.
/// Fornece operações CRUD padrão para qualquer entidade.
/// Desacopla DbContext da camada Application.
/// </summary>
public class GenericRepository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly AppDbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public GenericRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    /// <summary>
    /// Obtém entidade por ID com rastreamento
    /// </summary>
    public async Task<TEntity?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// Obtém todas as entidades com suporte a paginação
    /// </summary>
    public async Task<IEnumerable<TEntity>> GetAllAsync(int? skip = null, int? take = null)
    {
        IQueryable<TEntity> query = _dbSet.AsNoTracking();

        if (skip.HasValue && skip.Value > 0)
            query = query.Skip(skip.Value);

        if (take.HasValue && take.Value > 0)
            query = query.Take(take.Value);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Conta total de registros
    /// </summary>
    public async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

    /// <summary>
    /// Insere nova entidade
    /// </summary>
    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), "Entidade não pode ser nula");

        _dbSet.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Insere múltiplas entidades em uma operação
    /// </summary>
    public async Task InsertRangeAsync(IEnumerable<TEntity> entities)
    {
        if (entities == null || !entities.Any())
            throw new ArgumentException("Coleção de entidades não pode estar vazia", nameof(entities));

        _dbSet.AddRange(entities);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Atualiza entidade existente
    /// </summary>
    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity), "Entidade não pode ser nula");

        _context.Entry(entity).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Deleta entidade por ID
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null)
            return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Deleta entidade
    /// </summary>
    public async Task<bool> DeleteAsync(TEntity entity)
    {
        if (entity == null)
            return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Verifica existência de entidade
    /// </summary>
    public async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.FindAsync(id) != null;
    }

    /// <summary>
    /// Retorna IQueryable para queries customizadas
    /// Usa AsNoTracking para melhor performance em read-only
    /// </summary>
    public IQueryable<TEntity> Query()
    {
        return _dbSet.AsNoTracking();
    }
}
