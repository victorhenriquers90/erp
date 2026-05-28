using System.Net;
using System.Net.Http.Json;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using ProjetoVarejo.Tests.Builders;
using ProjetoVarejo.Tests.Integration;
using Xunit;

namespace ProjetoVarejo.Tests.Integration.Endpoints;

/// <summary>
/// Comprehensive integration tests for VendasEndpoints using WebApplicationFactory.
/// Tests complete sales CRUD operations with authentication and authorization.
/// </summary>
[Collection("Integration Tests")]
public class VendasEndpointsIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private HttpClient _authenticatedClient;
    private string _adminToken;
    private string _caixaToken;

    public VendasEndpointsIntegrationTests()
    {
        _fixture = new IntegrationTestFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _authenticatedClient = _fixture.CreateClient();

        // Authenticate as admin and caixa for tests
        _adminToken = await AuthenticateAsync("admin", "senha123");
        _caixaToken = await AuthenticateAsync("caixa", "senha123");
    }

    public async Task DisposeAsync()
    {
        _authenticatedClient?.Dispose();
        await _fixture.DisposeAsync();
    }

    private async Task<string> AuthenticateAsync(string usuario, string senha)
    {
        var loginRequest = new { usuario, senha };
        var response = await _authenticatedClient.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<LoginResponse>>();
        return content.Data.Token;
    }

    #region GET /api/vendas Tests

    [Fact]
    public async Task GetVendas_WithValidToken_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<PagedResult<dynamic>>>();
        Assert.True(content.Success);
    }

    [Fact]
    public async Task GetVendas_WithoutToken_Returns401()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetVendas_WithPagination_ReturnsPaginatedResult()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<PagedResult<dynamic>>>();
        Assert.True(content.Success);
        Assert.NotNull(content.Data);
    }

    [Fact]
    public async Task GetVendas_WithDateFilter_ReturnsFilteredResults()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var tomorrow = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd");

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/vendas?dataInicio={today}&dataFim={tomorrow}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region GET /api/vendas/{id} Tests

    [Fact]
    public async Task GetVendaById_ValidId_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act - Create a venda first
        var createResponse = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });
        var createdVenda = await createResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        var vendaId = createdVenda.Data.Id;

        // Act - Get the venda
        var getResponse = await _authenticatedClient.GetAsync($"/api/vendas/{vendaId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetVendaById_InvalidId_Returns404()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/vendas Tests

    [Fact]
    public async Task CreateVenda_WithValidToken_Returns201()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<dynamic>>();
        Assert.True(content.Success);
        Assert.NotNull(content.Data);
    }

    [Fact]
    public async Task CreateVenda_WithoutToken_Returns401()
    {
        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateVenda_AsUnauthenticatedRole_ReturnsForbidden()
    {
        // Arrange
        var estoquistaToken = await AuthenticateAsync("estoquista", "senha123");
        SetAuthorizationHeader(estoquistaToken);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region POST /api/vendas/{id}/items Tests

    [Fact]
    public async Task AddItemToVenda_ValidProduct_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Create venda
        var vendaResponse = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });
        var venda = await vendaResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        var vendaId = venda.Data.Id;

        var addItemRequest = new { produtoId = 1, quantidade = 5, precoUnitario = 100m };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/vendas/{vendaId}/items", addItemRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddItemToVenda_OutOfStock_Returns400()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        var vendaResponse = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });
        var venda = await vendaResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        var vendaId = venda.Data.Id;

        var addItemRequest = new { produtoId = 1, quantidade = 99999m, precoUnitario = 100m };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/vendas/{vendaId}/items", addItemRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region POST /api/vendas/{id}/finalize Tests

    [Fact]
    public async Task FinalizeVenda_WithItems_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Create venda and add item
        var vendaResponse = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });
        var venda = await vendaResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        var vendaId = venda.Data.Id;

        var addItemRequest = new { produtoId = 1, quantidade = 1, precoUnitario = 100m };
        await _authenticatedClient.PostAsJsonAsync($"/api/vendas/{vendaId}/items", addItemRequest);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/vendas/{vendaId}/finalize", new { });

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task FinalizeVenda_WithoutItems_Returns400()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        var vendaResponse = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });
        var venda = await vendaResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        var vendaId = venda.Data.Id;

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/vendas/{vendaId}/finalize", new { });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region POST /api/vendas/{id}/cancel Tests

    [Fact]
    public async Task CancelVenda_ValidVenda_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        var vendaResponse = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });
        var venda = await vendaResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        var vendaId = venda.Data.Id;

        var cancelRequest = new { motivo = "Solicitação do cliente" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/vendas/{vendaId}/cancel", cancelRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task CancelVenda_WithoutPermission_Returns403()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        var vendaResponse = await _authenticatedClient.PostAsJsonAsync("/api/vendas", new { });
        var venda = await vendaResponse.Content.ReadAsAsync<ApiResponse<dynamic>>();
        var vendaId = venda.Data.Id;

        var cancelRequest = new { motivo = "Solicitação do cliente" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync($"/api/vendas/{vendaId}/cancel", cancelRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Helper Methods

    private void SetAuthorizationHeader(string token)
    {
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    #endregion
}

public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
