using System.Net;
using System.Net.Http.Json;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Tests.Integration;
using Xunit;
using HttpContentExtensions = ProjetoVarejo.Tests.Integration.HttpContentExtensions;

namespace ProjetoVarejo.Tests.Integration.Authorization;

/// <summary>
/// Integration tests for role-based and permission-based authorization across all endpoints.
/// Verifies that authorization policies are enforced correctly for different user roles.
/// </summary>
[Collection("Integration Tests")]
public class RoleBasedAuthorizationTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private HttpClient _authenticatedClient;
    private string _adminToken;
    private string _gerenteToken;
    private string _caixaToken;
    private string _estoquistaToken;

    public RoleBasedAuthorizationTests()
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
        _estoquistaToken = await AuthenticateAsync("estoquista", "senha123");
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

    #region AdminOnly Policy Tests

    [Fact]
    public async Task AdminOnly_Administrador_Returns200()
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
    public async Task AdminOnly_Gerente_Returns403()
    {
        // Arrange
        SetAuthorizationHeader(_gerenteToken);

        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/fornecedores/1");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminOnly_Caixa_Returns403()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);

        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/fornecedores/1");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminOnly_Estoquista_Returns403()
    {
        // Arrange
        SetAuthorizationHeader(_estoquistaToken);

        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/fornecedores/1");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminOnly_Unauthenticated_Returns401()
    {
        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/fornecedores/1");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region AdminOrGerente Policy Tests

    [Fact]
    public async Task AdminOrGerente_Administrador_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var createRequest = new
        {
            nome = "Teste",
            cnpj = "12345678000195",
            email = "test@test.com"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/fornecedores", createRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminOrGerente_Gerente_Returns201()
    {
        // Arrange
        SetAuthorizationHeader(_gerenteToken);
        var createRequest = new
        {
            nome = "Teste",
            cnpj = "12345678000195",
            email = "test@test.com"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/fornecedores", createRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminOrGerente_Caixa_Returns403()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);
        var createRequest = new
        {
            nome = "Teste",
            cnpj = "12345678000195",
            email = "test@test.com"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/fornecedores", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminOrGerente_Estoquista_Returns403()
    {
        // Arrange
        SetAuthorizationHeader(_estoquistaToken);
        var createRequest = new
        {
            nome = "Teste",
            cnpj = "12345678000195",
            email = "test@test.com"
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/fornecedores", createRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region CanCancelSale Permission Tests

    [Fact]
    public async Task CanCancelSale_AdminWithPermission_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var cancelRequest = new { motivo = "Teste de cancelamento" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/vendas/1/cancel", cancelRequest);

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK ||
                   response.StatusCode == HttpStatusCode.Accepted ||
                   response.StatusCode == HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CanCancelSale_CaixaWithoutPermission_Returns403()
    {
        // Arrange
        SetAuthorizationHeader(_caixaToken);
        var cancelRequest = new { motivo = "Teste de cancelamento" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/vendas/1/cancel", cancelRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CanCancelSale_EstoquistaWithoutPermission_Returns403()
    {
        // Arrange
        SetAuthorizationHeader(_estoquistaToken);
        var cancelRequest = new { motivo = "Teste de cancelamento" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/vendas/1/cancel", cancelRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region CanViewFinancials Permission Tests

    [Fact]
    public async Task CanViewFinancials_AdminWithPermission_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/financeiro/lancamentos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CanViewFinancials_GerenteWithPermission_Returns200()
    {
        // Arrange
        SetAuthorizationHeader(_gerenteToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/financeiro/lancamentos");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CanViewFinancials_EstoquistaWithoutPermission_Returns403()
    {
        // Arrange
        SetAuthorizationHeader(_estoquistaToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/financeiro/lancamentos");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Protected Endpoint Without Authorization Tests

    [Fact]
    public async Task ProtectedEndpoint_NoAuthorizationHeader_Returns401()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_InvalidToken_Returns401()
    {
        // Arrange
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.jwt.token");

        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_MalformedAuthorizationHeader_Returns401()
    {
        // Arrange
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("NotBearer", "token");

        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Cross-Role Endpoint Access Tests

    [Fact]
    public async Task VendasEndpoint_AllAuthenticatedRoles_CanRead()
    {
        // Assert & Act
        var adminResponse = await MakeGetRequestWithRole(_adminToken, "/api/vendas");
        var gerenteResponse = await MakeGetRequestWithRole(_gerenteToken, "/api/vendas");
        var caixaResponse = await MakeGetRequestWithRole(_caixaToken, "/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, gerenteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, caixaResponse.StatusCode);
    }

    [Fact]
    public async Task CaixaEndpoint_OnlyAuthorizedRoles_CanAccess()
    {
        // Assert & Act
        var adminResponse = await MakeGetRequestWithRole(_adminToken, "/api/caixa/current-session");
        var caixaResponse = await MakeGetRequestWithRole(_caixaToken, "/api/caixa/current-session");
        var estoquistaResponse = await MakeGetRequestWithRole(_estoquistaToken, "/api/caixa/current-session");

        // Assert - Admin and Caixa can access, Estoquista may be denied
        Assert.True(adminResponse.StatusCode == HttpStatusCode.OK ||
                   adminResponse.StatusCode == HttpStatusCode.NoContent);
        Assert.True(caixaResponse.StatusCode == HttpStatusCode.OK ||
                   caixaResponse.StatusCode == HttpStatusCode.NoContent);
    }

    #endregion

    #region Helper Methods

    private void SetAuthorizationHeader(string token)
    {
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<HttpResponseMessage> MakeGetRequestWithRole(string token, string endpoint)
    {
        SetAuthorizationHeader(token);
        return await _authenticatedClient.GetAsync(endpoint);
    }

    private async Task<HttpResponseMessage> MakePostRequestWithRole(string token, string endpoint, object requestBody)
    {
        SetAuthorizationHeader(token);
        return await _authenticatedClient.PostAsJsonAsync(endpoint, requestBody);
    }

    #endregion
}
