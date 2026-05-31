using FluentAssertions;
using ProjetoVarejo.Desktop.Helpers;
using Xunit;

namespace ProjetoVarejo.Tests;

public class GridPaginationTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        var pagination = new GridPagination();

        pagination.PageSize.Should().Be(10);
        pagination.CurrentPage.Should().Be(1);
        pagination.TotalRecords.Should().Be(0);
    }

    [Fact]
    public void Offset_ShouldCalculateCorrectly()
    {
        var pagination = new GridPagination { PageSize = 10, CurrentPage = 2 };

        pagination.Offset.Should().Be(10); // (2-1)*10 = 10
    }

    [Fact]
    public void TotalPages_ShouldCalculateCorrectly()
    {
        var pagination = new GridPagination { PageSize = 10, TotalRecords = 25 };

        pagination.TotalPages.Should().Be(3); // ceil(25/10) = 3
    }

    [Fact]
    public void CanGoNext_ShouldReturnTrueWhenNotLastPage()
    {
        var pagination = new GridPagination 
        { 
            PageSize = 10, 
            TotalRecords = 25, 
            CurrentPage = 1 
        };

        pagination.CanGoNext.Should().BeTrue();
    }

    [Fact]
    public void CanGoNext_ShouldReturnFalseWhenLastPage()
    {
        var pagination = new GridPagination 
        { 
            PageSize = 10, 
            TotalRecords = 25, 
            CurrentPage = 3 
        };

        pagination.CanGoNext.Should().BeFalse();
    }

    [Fact]
    public void CanGoPrevious_ShouldReturnFalseWhenFirstPage()
    {
        var pagination = new GridPagination { CurrentPage = 1 };

        pagination.CanGoPrevious.Should().BeFalse();
    }

    [Fact]
    public void CanGoPrevious_ShouldReturnTrueWhenNotFirstPage()
    {
        var pagination = new GridPagination { CurrentPage = 2 };

        pagination.CanGoPrevious.Should().BeTrue();
    }

    [Fact]
    public void NextPage_ShouldIncreaseCurrentPage()
    {
        var pagination = new GridPagination { PageSize = 10, TotalRecords = 25 };

        pagination.NextPage();

        pagination.CurrentPage.Should().Be(2);
    }

    [Fact]
    public void NextPage_ShouldNotExceedLastPage()
    {
        var pagination = new GridPagination 
        { 
            PageSize = 10, 
            TotalRecords = 25, 
            CurrentPage = 3 
        };

        pagination.NextPage();

        pagination.CurrentPage.Should().Be(3);
    }

    [Fact]
    public void PreviousPage_ShouldDecreaseCurrentPage()
    {
        var pagination = new GridPagination { CurrentPage = 2 };

        pagination.PreviousPage();

        pagination.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void PreviousPage_ShouldNotGoBeforeFirstPage()
    {
        var pagination = new GridPagination { CurrentPage = 1 };

        pagination.PreviousPage();

        pagination.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void FirstPage_ShouldSetCurrentPageTo1()
    {
        var pagination = new GridPagination { CurrentPage = 5 };

        pagination.FirstPage();

        pagination.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void LastPage_ShouldSetCurrentPageToLastPage()
    {
        var pagination = new GridPagination 
        { 
            PageSize = 10, 
            TotalRecords = 25 
        };

        pagination.LastPage();

        pagination.CurrentPage.Should().Be(3);
    }

    [Fact]
    public void GoToPage_ShouldSetCurrentPageWhenValid()
    {
        var pagination = new GridPagination 
        { 
            PageSize = 10, 
            TotalRecords = 25 
        };

        pagination.GoToPage(2);

        pagination.CurrentPage.Should().Be(2);
    }

    [Fact]
    public void GoToPage_ShouldNotSetInvalidPageNumber()
    {
        var pagination = new GridPagination 
        { 
            PageSize = 10, 
            TotalRecords = 25 
        };

        pagination.GoToPage(10);

        pagination.CurrentPage.Should().Be(1);
    }

    [Fact]
    public void ToString_ShouldFormatCorrectly()
    {
        var pagination = new GridPagination 
        { 
            PageSize = 10, 
            TotalRecords = 25, 
            CurrentPage = 2 
        };

        pagination.ToString().Should().Be("11-20 de 25");
    }

    [Fact]
    public void Reset_ShouldResetToPrimaryState()
    {
        var pagination = new GridPagination 
        { 
            CurrentPage = 5, 
            TotalRecords = 100 
        };

        pagination.Reset();

        pagination.CurrentPage.Should().Be(1);
        pagination.TotalRecords.Should().Be(0);
    }

    [Fact]
    public void PageSize_ShouldNotAllowNegativeValues()
    {
        var pagination = new GridPagination { PageSize = -5 };

        pagination.PageSize.Should().Be(10); // Default value
    }

    [Fact]
    public void CurrentPage_ShouldNotAllowNegativeValues()
    {
        var pagination = new GridPagination { CurrentPage = -1 };

        pagination.CurrentPage.Should().Be(1); // Default value
    }
}
