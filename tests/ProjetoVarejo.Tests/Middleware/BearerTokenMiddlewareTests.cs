using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using ProjetoVarejo.Api.Configuration;
using ProjetoVarejo.Api.Middleware;
using ProjetoVarejo.Api.Services;
using ProjetoVarejo.Tests.Builders;

namespace ProjetoVarejo.Tests.Middleware;

/// <summary>
/// Unit tests for BearerTokenMiddleware JWT token validation.
/// Tests token extraction, validation, and context user setup.
/// </summary>
public class BearerTokenMiddlewareTests
{
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly BearerTokenMiddleware _middleware;

    public BearerTokenMiddlewareTests()
    {
        _mockTokenService = new Mock<ITokenService>();
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new BearerTokenMiddleware(_mockNext.Object);
    }

    private HttpContext CreateHttpContext()
    {
        return new DefaultHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_ValidBearerToken_SetsUserPrincipal()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var usuario = UsuarioBuilder.CreateAdmin(1);
        var token = "valid.jwt.token";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Name, "admin")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockTokenService.Setup(ts => ts.ValidateToken(token)).Returns(principal);
        _mockNext.Setup(n => n.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTokenService.Object);

        // Assert
        Assert.NotNull(httpContext.User);
        Assert.Equal(principal, httpContext.User);
        _mockNext.Verify(n => n.Invoke(httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_InvalidBearerToken_Returns401()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var token = "invalid.jwt.token";
        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockTokenService.Setup(ts => ts.ValidateToken(token)).Returns((ClaimsPrincipal?)null);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTokenService.Object);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        _mockNext.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_ExpiredToken_Returns401()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var token = "expired.jwt.token";
        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockTokenService.Setup(ts => ts.ValidateToken(token)).Returns((ClaimsPrincipal?)null);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTokenService.Object);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_MissingAuthorizationHeader_CallsNext()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        _mockNext.Setup(n => n.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTokenService.Object);

        // Assert
        _mockNext.Verify(n => n.Invoke(httpContext), Times.Once);
        // User should not be set by middleware when no auth header
        Assert.Null(httpContext.User?.Identity?.Name);
    }

    [Fact]
    public async Task InvokeAsync_NonBearerAuthHeader_CallsNext()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Authorization"] = "Basic dXNlcjpwYXNz";
        _mockNext.Setup(n => n.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTokenService.Object);

        // Assert
        // Non-Bearer auth should pass through to next middleware
        _mockNext.Verify(n => n.Invoke(httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_BearerWithoutToken_CallsNext()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer ";
        _mockNext.Setup(n => n.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTokenService.Object);

        // Assert
        _mockNext.Verify(n => n.Invoke(httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ValidToken_ExtractsUserClaims()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var token = "valid.jwt.token";
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "42"),
            new(ClaimTypes.Name, "gerente_teste"),
            new(ClaimTypes.Role, "Gerente")
        };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockTokenService.Setup(ts => ts.ValidateToken(token)).Returns(principal);
        _mockNext.Setup(n => n.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTokenService.Object);

        // Assert
        Assert.NotNull(httpContext.User);
        Assert.Equal("42", httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal("gerente_teste", httpContext.User.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal("Gerente", httpContext.User.FindFirst(ClaimTypes.Role)?.Value);
    }

    [Fact]
    public async Task InvokeAsync_ValidToken_DoesNotCallTokenServiceMultipleTimes()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var token = "valid.jwt.token";
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "1") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));

        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockTokenService.Setup(ts => ts.ValidateToken(token)).Returns(principal);
        _mockNext.Setup(n => n.Invoke(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTokenService.Object);

        // Assert
        _mockTokenService.Verify(ts => ts.ValidateToken(token), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_InvalidTokenResponse_HasJsonContentType()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        var token = "invalid.jwt.token";
        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        _mockTokenService.Setup(ts => ts.ValidateToken(token)).Returns((ClaimsPrincipal?)null);

        // Act
        await _middleware.InvokeAsync(httpContext, _mockTokenService.Object);

        // Assert
        Assert.StartsWith("application/json", httpContext.Response.ContentType);
    }
}
