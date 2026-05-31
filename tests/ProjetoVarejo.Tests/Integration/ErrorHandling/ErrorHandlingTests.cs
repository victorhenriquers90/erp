using System.Net;
using System.Net.Http.Json;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Tests.Integration;
using Xunit;
using HttpContentExtensions = ProjetoVarejo.Tests.Integration.HttpContentExtensions;

namespace ProjetoVarejo.Tests.Integration.ErrorHandling;

/// <summary>
/// Integration tests for error handling and response standardization across the API.
/// Verifies that all error scenarios return consistent ApiResponse format with correct status codes.
/// </summary>
[Collection("Integration Tests")]
public class ErrorHandlingTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private HttpClient _authenticatedClient;
    private string _adminToken;

    public ErrorHandlingTests()
    {
        _fixture = new IntegrationTestFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _authenticatedClient = _fixture.CreateClient();

        _adminToken = await AuthenticateAsync("admin", "senha123");
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

    #region 400 Bad Request Tests

    [Fact]
    public async Task InvalidRequest_MissingRequiredField_Returns400()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var invalidRequest = new { };  // Missing required fields

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/fornecedores", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse>();
        Assert.False(content.Success);
    }

    [Fact]
    public async Task InvalidRequest_MalformedJson_Returns400()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var content = new StringContent("{invalid json}", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _authenticatedClient.PostAsync("/api/fornecedores", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidRequest_NegativeQuantity_Returns400()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var invalidRequest = new { valor = -100m };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/caixa/supply", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidLogin_EmptyUsername_Returns400()
    {
        // Arrange
        var loginRequest = new { usuario = "", senha = "senha123" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse>();
        Assert.False(content.Success);
    }

    [Fact]
    public async Task InvalidLogin_EmptyPassword_Returns400()
    {
        // Arrange
        var loginRequest = new { usuario = "admin", senha = "" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region 401 Unauthorized Tests

    [Fact]
    public async Task MissingAuthorizationHeader_ProtectedEndpoint_Returns401()
    {
        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse>();
        Assert.False(content.Success);
    }

    [Fact]
    public async Task InvalidToken_ProtectedEndpoint_Returns401()
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
    public async Task ExpiredToken_ProtectedEndpoint_Returns401()
    {
        // Arrange
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE1MTYyMzkwMjJ9.invalid";
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task InvalidLogin_WrongPassword_Returns400()
    {
        // Arrange
        var loginRequest = new { usuario = "admin", senha = "wrongpassword" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region 403 Forbidden Tests

    [Fact]
    public async Task UnauthorizedRole_ProtectedEndpoint_Returns403()
    {
        // Arrange
        var caixaToken = await AuthenticateAsync("caixa", "senha123");
        SetAuthorizationHeader(caixaToken);

        // Act
        var response = await _authenticatedClient.DeleteAsync("/api/fornecedores/1");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse>();
        Assert.False(content.Success);
    }

    [Fact]
    public async Task MissingPermission_ProtectedEndpoint_Returns403()
    {
        // Arrange
        var estoquistaToken = await AuthenticateAsync("estoquista", "senha123");
        SetAuthorizationHeader(estoquistaToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/financeiro/lancamentos");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region 404 Not Found Tests

    [Fact]
    public async Task NonexistentResource_GetById_Returns404()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse>();
        Assert.False(content.Success);
    }

    [Fact]
    public async Task NonexistentEndpoint_Returns404()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/nonexistent/endpoint");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region Error Response Format Tests

    [Fact]
    public async Task ErrorResponse_HasConsistentFormat()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas/999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse>();
        Assert.NotNull(content);
        Assert.False(content.Success);
        Assert.NotNull(content.Message);
        Assert.NotNull(content.Timestamp);
    }

    [Fact]
    public async Task ErrorResponse_IncludesErrorCode()
    {
        // Arrange
        var loginRequest = new { usuario = "admin", senha = "wrongpassword" };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        var content = await response.Content.ReadAsAsync<ApiResponse>();
        Assert.NotNull(content.ErrorCode);
        Assert.True(content.ErrorCode == 400 || content.ErrorCode > 0);
    }

    [Fact]
    public async Task SuccessResponse_HasCorrectStructure()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act
        var response = await _authenticatedClient.GetAsync("/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse<dynamic>>();
        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.NotNull(content.Timestamp);
    }

    [Fact]
    public async Task ValidationError_IncludesErrorsList()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var invalidRequest = new { cnpj = "invalid-cnpj" };  // Invalid CNPJ format

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync("/api/fornecedores", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsAsync<ApiResponse>();
        Assert.NotNull(content.Errors);
    }

    #endregion

    #region HTTP Method Tests

    [Fact]
    public async Task MethodNotAllowed_WrongHttpMethod_ReturnsError()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);

        // Act - Try GET on POST-only endpoint (if applicable)
        var response = await _authenticatedClient.GetAsync("/api/auth/login");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.MethodNotAllowed ||
                   response.StatusCode == HttpStatusCode.BadRequest);
    }

    #endregion

    #region Concurrent Request Tests

    [Fact]
    public async Task ConcurrentRequests_MultipleClients_AllSucceed()
    {
        // Arrange
        SetAuthorizationHeader(_adminToken);
        var tasks = Enumerable.Range(1, 10)
            .Select(_ => _authenticatedClient.GetAsync("/api/vendas"))
            .ToList();

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        Assert.All(responses, response =>
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        });
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
