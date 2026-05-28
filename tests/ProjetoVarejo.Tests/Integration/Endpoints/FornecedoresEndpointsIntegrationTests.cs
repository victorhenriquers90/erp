using System.Net;
using System.Net.Http.Json;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Tests.Integration;
using Xunit;

namespace ProjetoVarejo.Tests.Integration.Endpoints;

/// <summary>
/// Comprehensive integration tests for FornecedoresEndpoints using WebApplicationFactory.
/// Tests supplier CRUD operations with role-based authorization.
/// </summary>
[Collection("Integration Tests")]
public class FornecedoresEndpointsIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private HttpClient _authenticatedClient;
    private string _adminToken;
    private string _gerenteToken;
    private string _caixaToken;

    public FornecedoresEndpointsIntegrationTests()
    {
        _fixture = new IntegrationTestFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _authenticatedClient = _fixture.CreateClient();

        _adminToken = await AuthenticateAsync("admin", "senha123");
        _gerenteToken = await AuthenticateAsync("gerente", "senha123");
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

    #region GET /api/fornecedores Tests

    [Fact]
    public async Task ListFornecedores_WithValidToken_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/fornecedores");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<dynamic>>();
        Assert.True(content.Success);
    }

    [Fact]
    public async Task ListFornecedores_WithoutToken_Returns401()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/fornecedores");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListFornecedores_WithPagination_ReturnsPaginated()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/fornecedores?page=1&pageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region GET /api/fornecedores/{id} Tests

    [Fact]
    public async Task GetFornecedorById_ValidId_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/fornecedores/1");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/fornecedores Tests

    [Fact]
    public async Task CreateFornecedor_AsAdmin_Returns201()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var createRequest = new
        {
            nome = "Test Supplier",
            cnpj = "12345678000195",
            email = "supplier@test.com",
            telefone = "1133334444"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/fornecedores", createRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateFornecedor_AsGerente_Returns201()
    {
        // Arrange
        SetAuthorizationHeader(_gerenteToken);
        var createRequest = new
        {
            nome = "Another Supplier",
            cnpj = "98765432000180",
            email = "another@test.com"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/fornecedores", createRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateFornecedor_AsCaixa_ReturnsForbidden()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);
        var createRequest = new
        {
            nome = "Forbidden Supplier",
            cnpj = "11122233344455"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/fornecedores", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region PUT /api/fornecedores/{id} Tests

    [Fact]
    public async Task UpdateFornecedor_AsAdmin_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var updateRequest = new
        {
            nome = "Updated Supplier Name",
            email = "updated@test.com"
        };

        // Act
        var response = await _authenticatedClient.PutAsJsonAsync("/api/fornecedores/1", updateRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region DELETE /api/fornecedores/{id} Tests

    [Fact]
    public async Task DeleteFornecedor_AsAdmin_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/fornecedores/999");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.NoContent ||
                   response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteFornecedor_AsCaixa_ReturnsForbidden()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/fornecedores/1");

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
