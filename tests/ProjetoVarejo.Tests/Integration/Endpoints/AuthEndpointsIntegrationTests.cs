using System.Net;
using System.Net.Http.Json;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Tests.Builders;
using ProjetoVarejo.Tests.Integration;
using Xunit;

namespace ProjetoVarejo.Tests.Integration.Endpoints;

/// <summary>
/// Comprehensive integration tests for AuthEndpoints using WebApplicationFactory.
/// Tests complete HTTP request/response cycle with JWT authentication and real database.
/// </summary>
[Collection("Integration Tests")]
public class AuthEndpointsIntegrationTests : IAsyncLifetime
{
    private readonly IntegrationTestFixture _fixture;
    private HttpClient _client;

    public AuthEndpointsIntegrationTests()
    {
        _fixture = new IntegrationTestFixture();
    }

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        _client = _fixture.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        await _fixture.DisposeAsync();
    }

    #region Login Endpoint Tests

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithAccessToken()
    {
        // Arrange
        var loginRequest = new { usuario = "admin", senha = "senha123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<LoginResponse>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.True(content.Success);
        Assert.NotNull(content.Data);
        Assert.NotEmpty(content.Data.Token);
        Assert.NotEmpty(content.Data.RefreshToken);
        Assert.Equal(3600, content.Data.ExpiresIn);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsUserInfo()
    {
        // Arrange
        var loginRequest = new { usuario = "admin", senha = "senha123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<LoginResponse>>();

        // Assert
        Assert.True(content.Success);
        Assert.NotNull(content.Data);
        Assert.True(content.Data.UsuarioId > 0);
        Assert.NotEmpty(content.Data.UsuarioNome);
        Assert.Equal("Administrador", content.Data.UsuarioPerfil);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns400()
    {
        // Arrange
        var loginRequest = new { usuario = "admin", senha = "wrongpassword" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<LoginResponse>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(content.Success);
        Assert.Null(content.Data);
    }

    [Fact]
    public async Task Login_NonexistentUser_Returns400()
    {
        // Arrange
        var loginRequest = new { usuario = "nonexistent", senha = "senha123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<LoginResponse>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(content.Success);
    }

    [Fact]
    public async Task Login_EmptyUsername_Returns400()
    {
        // Arrange
        var loginRequest = new { usuario = "", senha = "senha123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<LoginResponse>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(content.Success);
    }

    [Fact]
    public async Task Login_EmptyPassword_Returns400()
    {
        // Arrange
        var loginRequest = new { usuario = "admin", senha = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<LoginResponse>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(content.Success);
    }

    [Fact]
    public async Task Login_ValidToken_AllowsAccessToProtectedEndpoint()
    {
        // Arrange - Login and get token
        var loginRequest = new { usuario = "admin", senha = "senha123" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsAsync<ApiResponse<LoginResponse>>();
        var token = loginContent.Data.Token;

        // Act - Use token to access protected endpoint
        var protectedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/vendas");
        protectedRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        var protectedResponse = await _client.SendAsync(protectedRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/vendas");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", invalidToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_NoAuthorizationHeader_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/vendas");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Refresh Endpoint Tests

    [Fact]
    public async Task Refresh_ValidRefreshToken_Returns200WithNewAccessToken()
    {
        // Arrange - First login to get refresh token
        var loginRequest = new { usuario = "admin", senha = "senha123" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginContent = await loginResponse.Content.ReadAsAsync<ApiResponse<LoginResponse>>();
        var refreshToken = loginContent.Data.RefreshToken;

        var refreshRequest = new { refreshToken = refreshToken };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<RefreshResponse>>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(content.Success);
        Assert.NotNull(content.Data);
        Assert.NotEmpty(content.Data.Token);
        Assert.Equal(3600, content.Data.ExpiresIn);
    }

    [Fact]
    public async Task Refresh_InvalidRefreshToken_Returns400()
    {
        // Arrange
        var refreshRequest = new { refreshToken = "invalid.token" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<RefreshResponse>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(content.Success);
    }

    [Fact]
    public async Task Refresh_EmptyRefreshToken_Returns400()
    {
        // Arrange
        var refreshRequest = new { refreshToken = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh", refreshRequest);
        var content = await response.Content.ReadAsAsync<ApiResponse<RefreshResponse>>();

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(content.Success);
    }

    #endregion
}

/// <summary>
/// Helper class for reading JSON response content from HttpResponseMessage.
/// </summary>
internal static class HttpContentExtensions
{
    public static async Task<T> ReadAsAsync<T>(this HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return System.Text.Json.JsonSerializer.Deserialize<T>(json,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

/// <summary>
/// Data transfer objects for API responses.
/// </summary>
public class LoginResponse
{
    public int UsuarioId { get; set; }
    public string UsuarioNome { get; set; }
    public string UsuarioPerfil { get; set; }
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}

public class RefreshResponse
{
    public string Token { get; set; }
    public int ExpiresIn { get; set; }
}
