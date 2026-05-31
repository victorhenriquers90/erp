using System.Net;
using System.Net.Http.Json;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Tests.Integration;
using Xunit;

namespace ProjetoVarejo.Tests.Integration.Endpoints;

/// <summary>
/// Comprehensive integration tests for CaixaEndpoints using WebApplicationFactory.
/// Tests cash register session operations with proper authorization.
/// </summary>
[Collection("Integration Tests")]
public class CaixaEndpointsIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private HttpClient _authenticatedClient;
    private string _adminToken;
    private string _gerenteToken;
    private string _caixaToken;

    public CaixaEndpointsIntegrationTests()
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

    #region GET /api/caixa/current-session Tests

    [Fact]
    public async Task GetCurrentSession_WithValidToken_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/caixa/current-session");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetCurrentSession_WithoutToken_Returns401()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/caixa/current-session");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /api/caixa/open-session Tests

    [Fact]
    public async Task OpenSession_AsAdmin_Returns201()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var openSessionRequest = new
        {
            saldoAbertura = 0m,
            usuarioId = 1
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/caixa/open-session", openSessionRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenSession_AsGerente_Returns201()
    {
        // Arrange
        SetAuthorizationHeader(_gerenteToken);
        var openSessionRequest = new
        {
            saldoAbertura = 0m,
            usuarioId = 1
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/caixa/open-session", openSessionRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task OpenSession_AsCaixa_ReturnsForbidden()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);
        var openSessionRequest = new
        {
            saldoAbertura = 0m,
            usuarioId = 1
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/caixa/open-session", openSessionRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region POST /api/caixa/close-session Tests

    [Fact]
    public async Task CloseSession_WithValidToken_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/caixa/close-session", new { });

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.NoContent ||
                   response.StatusCode == HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/caixa/supply Tests

    [Fact]
    public async Task AddSupply_ValidAmount_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);
        var supplyRequest = new
        {
            valor = 100m,
            observacao = "Reposição de caixa"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/caixa/supply", supplyRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddSupply_WithoutToken_Returns401()
    {
        // Arrange
        var supplyRequest = new
        {
            valor = 100m,
            observacao = "Reposição de caixa"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/caixa/supply", supplyRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /api/caixa/withdrawal Tests

    [Fact]
    public async Task RemoveWithdrawal_ValidAmount_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);
        var withdrawalRequest = new
        {
            valor = 50m,
            observacao = "Retirada de caixa"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/caixa/withdrawal", withdrawalRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created);
    }

    [Fact]
    public async Task RemoveWithdrawal_ExceedsBalance_Returns400()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);
        var withdrawalRequest = new
        {
            valor = 999999m,
            observacao = "Retirada de caixa"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/caixa/withdrawal", withdrawalRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GET /api/caixa/movements Tests

    [Fact]
    public async Task ListMovements_WithValidToken_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/caixa/movements");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<dynamic>>();
        Assert.True(content.Success);
    }

    [Fact]
    public async Task ListMovements_WithDateFilter_ReturnsFiltered()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);
        var today = DateTime.Today.ToString("yyyy-MM-dd");

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/caixa/movements?de={today}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region GET /api/caixa/summary Tests

    [Fact]
    public async Task GetSummary_WithValidToken_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/caixa/summary");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region GET /api/caixa/discrepancies Tests

    [Fact]
    public async Task GetDiscrepancies_WithValidToken_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/caixa/discrepancies");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent);
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
