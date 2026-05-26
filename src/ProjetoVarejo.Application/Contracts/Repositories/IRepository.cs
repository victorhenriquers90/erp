using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Contracts.Repositories;

/// <summary>
/// Interface genérica para repositório de dados.
/// Abstrai acesso a dados e permite injeção de dependência.
/// Implementa padrão Repository Pattern para desacoplamento de DbContext.
/// </summary>
/// <typeparam name="TEntity">Tipo da entidade (deve herdar de EntidadeBase)</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Obtém uma entidade por ID
    /// </summary>
    Task<TEntity?> GetByIdAsync(int id);

    /// <summary>
    /// Obtém todas as entidades com paginação
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync(int? skip = null, int? take = null);

    /// <summary>
    /// Obtém número total de registros
    /// </summary>
    Task<int> CountAsync();

    /// <summary>
    /// Insere uma nova entidade
    /// </summary>
    Task<TEntity> InsertAsync(TEntity entity);

    /// <summary>
    /// Insere múltiplas entidades
    /// </summary>
    Task InsertRangeAsync(IEnumerable<TEntity> entities);

    /// <summary>
    /// Atualiza uma entidade existente
    /// </summary>
    Task<TEntity> UpdateAsync(TEntity entity);

    /// <summary>
    /// Deleta uma entidade por ID
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Deleta uma entidade
    /// </summary>
    Task<bool> DeleteAsync(TEntity entity);

    /// <summary>
    /// Verifica se uma entidade existe
    /// </summary>
    Task<bool> ExistsAsync(int id);

    /// <summary>
    /// Retorna queryable para operações customizadas (read-only)
    /// </summary>
    IQueryable<TEntity> Query();
}
