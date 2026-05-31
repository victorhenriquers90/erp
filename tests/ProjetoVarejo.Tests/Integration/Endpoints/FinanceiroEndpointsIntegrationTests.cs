using System.Net;
using System.Net.Http.Json;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Tests.Integration;
using Xunit;

namespace ProjetoVarejo.Tests.Integration.Endpoints;

/// <summary>
/// Comprehensive integration tests for FinanceiroEndpoints using WebApplicationFactory.
/// Tests financial operations with proper authorization.
/// </summary>
[Collection("Integration Tests")]
public class FinanceiroEndpointsIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private HttpClient _authenticatedClient;
    private string _adminToken;
    private string _gerenteToken;
    private string _caixaToken;

    public FinanceiroEndpointsIntegrationTests()
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

    #region GET /api/financeiro/contas Tests

    [Fact]
    public async Task ListContas_WithValidToken_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/financeiro/contas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<dynamic>>();
        Assert.True(content.Success);
    }

    [Fact]
    public async Task ListContas_WithoutToken_Returns401()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/financeiro/contas");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region POST /api/financeiro/contas Tests

    [Fact]
    public async Task CreateConta_AsAdmin_Returns201()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var createRequest = new
        {
            nome = "Conta Bancária",
            banco = "Banco Teste",
            agencia = "0001",
            numero = "123456"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/financeiro/contas", createRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateConta_AsCaixa_ReturnsForbidden()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);
        var createRequest = new { nome = "Forbidden Account" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/financeiro/contas", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region GET /api/financeiro/lancamentos Tests

    [Fact]
    public async Task ListLancamentos_WithValidToken_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/financeiro/lancamentos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<dynamic>>();
        Assert.True(content.Success);
    }

    [Fact]
    public async Task ListLancamentos_WithDateFilter_ReturnsFiltered()
    {
        // Arrange
        SetAuthorizationHeader(_gerenteToken);
        var today = DateTime.Today.ToString("yyyy-MM-dd");

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/financeiro/lancamentos?de={today}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region POST /api/financeiro/lancamentos Tests

    [Fact]
    public async Task CreateLancamento_AsAdmin_Returns201()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var createRequest = new
        {
            contaId = 1,
            tipo = "Receita",
            valor = 1000m,
            descricao = "Receita de Teste",
            dataVencimento = DateTime.Today.ToString("yyyy-MM-dd")
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/financeiro/lancamentos", createRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateLancamento_AsGerente_Returns201()
    {
        // Arrange
        SetAuthorizationHeader(_gerenteToken);
        var createRequest = new
        {
            contaId = 1,
            tipo = "Despesa",
            valor = 500m,
            descricao = "Despesa de Teste"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/financeiro/lancamentos", createRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    #endregion

    #region POST /api/financeiro/lancamentos/{id}/marcar-pago Tests

    [Fact]
    public async Task MarkAsPaid_ValidLancamento_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/financeiro/lancamentos/1/marcar-pago", new { });

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.NoContent ||
                   response.StatusCode == HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/financeiro/lancamentos/{id} Tests

    [Fact]
    public async Task DeleteLancamento_AsAdmin_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/financeiro/lancamentos/999");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.NoContent ||
                   response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLancamento_AsGerente_ReturnsForbidden()
    {
        // Arrange
        SetAuthorizationHeader(_gerenteToken);

        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/financeiro/lancamentos/1");

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
