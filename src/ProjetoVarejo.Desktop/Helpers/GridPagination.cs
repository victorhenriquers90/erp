namespace ProjetoVarejo.Desktop.Helpers;

/// <summary>
/// Helper para paginação de grids em aplicações desktop.
/// Gerencia offset, limit, total de registros e cálculos de páginas.
/// </summary>
public class GridPagination
{
    private int _pageSize = 10;
    private int _currentPage = 1;
    private int _totalRecords;

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 0 ? value : 10;
    }

    public int CurrentPage
    {
        get => _currentPage;
        set => _currentPage = value > 0 ? value : 1;
    }

    public int TotalRecords
    {
        get => _totalRecords;
        set => _totalRecords = value >= 0 ? value : 0;
    }

    public int Offset => (CurrentPage - 1) * PageSize;

    public int TotalPages => TotalRecords == 0 ? 1 : (int)Math.Ceiling((double)TotalRecords / PageSize);

    public bool CanGoNext => CurrentPage < TotalPages;

    public bool CanGoPrevious => CurrentPage > 1;

    public bool CanGoFirst => CurrentPage > 1;

    public bool CanGoLast => CurrentPage < TotalPages;

    public int StartRecord => TotalRecords == 0 ? 0 : Offset + 1;

    public int EndRecord => Math.Min(Offset + PageSize, TotalRecords);

    /// <summary>Navega para a página anterior.</summary>
    public void PreviousPage()
    {
        if (CanGoPrevious)
            CurrentPage--;
    }

    /// <summary>Navega para a próxima página.</summary>
    public void NextPage()
    {
        if (CanGoNext)
            CurrentPage++;
    }

    /// <summary>Navega para a primeira página.</summary>
    public void FirstPage()
    {
        CurrentPage = 1;
    }

    /// <summary>Navega para a última página.</summary>
    public void LastPage()
    {
        CurrentPage = TotalPages;
    }

    /// <summary>Navega para uma página específica.</summary>
    public void GoToPage(int pageNumber)
    {
        if (pageNumber > 0 && pageNumber <= TotalPages)
            CurrentPage = pageNumber;
    }

    /// <summary>Reset pagination para a primeira página com 0 registros.</summary>
    public void Reset()
    {
        CurrentPage = 1;
        TotalRecords = 0;
    }

    /// <summary>Retorna string formatada: "1-10 de 100".</summary>
    public override string ToString()
    {
        if (TotalRecords == 0)
            return "0 de 0";
        return $"{StartRecord}-{EndRecord} de {TotalRecords}";
    }
}
