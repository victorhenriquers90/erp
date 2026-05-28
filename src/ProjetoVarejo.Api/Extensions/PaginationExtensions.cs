using ProjetoVarejo.Api.Models;

namespace ProjetoVarejo.Api.Extensions;

/// <summary>
/// Extension methods for pagination support on list endpoints.
/// Provides convenient methods for converting collections to paginated results.
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// Paginate a collection into a PagedResult.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    /// <param name="items">The source collection to paginate</param>
    /// <param name="page">The page number (1-based). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, max is 500.</param>
    /// <returns>PagedResult<T> with pagination metadata</returns>
    public static PagedResult<T> Paginate<T>(this IEnumerable<T> items, int page = 1, int pageSize = 50)
    {
        // Validate and normalize page and pageSize
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 500) pageSize = 500;

        var totalCount = items.Count();

        var paginatedItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>
        {
            Items = paginatedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Paginate a queryable collection into a PagedResult (more efficient for IQueryable).
    /// </summary>
    /// <typeparam name="T">The type of items in the collection</typeparam>
    /// <param name="items">The source IQueryable to paginate</param>
    /// <param name="page">The page number (1-based). Default is 1.</param>
    /// <param name="pageSize">The number of items per page. Default is 50, max is 500.</param>
    /// <returns>PagedResult<T> with pagination metadata</returns>
    public static PagedResult<T> Paginate<T>(this IQueryable<T> items, int page = 1, int pageSize = 50)
    {
        // Validate and normalize page and pageSize
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 500) pageSize = 500;

        var totalCount = items.Count();

        var paginatedItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>
        {
            Items = paginatedItems,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}
