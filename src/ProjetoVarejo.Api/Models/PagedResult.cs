namespace ProjetoVarejo.Api.Models;

/// <summary>
/// Pagination wrapper for list endpoints.
/// Contains items and metadata for pagination calculation.
/// </summary>
/// <typeparam name="T">The type of items in the list</typeparam>
public class PagedResult<T>
{
    /// <summary>Gets or sets the list of items for the current page.</summary>
    public List<T> Items { get; set; } = new();

    /// <summary>Gets or sets the total number of items across all pages.</summary>
    public int TotalCount { get; set; }

    /// <summary>Gets or sets the current page number (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Gets or sets the number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Gets the total number of pages.</summary>
    public int TotalPages => TotalCount == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;

    /// <summary>Gets a value indicating whether there is a next page.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Gets a value indicating whether there is a previous page.</summary>
    public bool HasPreviousPage => Page > 1;
}
