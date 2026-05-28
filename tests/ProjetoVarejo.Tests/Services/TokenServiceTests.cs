using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using ProjetoVarejo.Api.Configuration;
using ProjetoVarejo.Api.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using ProjetoVarejo.Tests.Builders;

namespace ProjetoVarejo.Tests.Services;

/// <summary>
/// Comprehensive unit tests for TokenService JWT generation and validation.
/// Tests token creation, claims extraction, expiration, and signature validation.
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly JwtSettings _jwtSettings;

    public TokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "test-secret-key-must-be-at-least-32-characters-long-for-hs256",
            Issuer = "ProjetoVarejo.Api",
            Audience = "ProjetoVarejo.Client",
            ExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };

        var options = Options.Create(_jwtSettings);
        _tokenService = new TokenService(options);
    }

    #region GenerateAccessToken Tests

    [Fact]
    public void GenerateAccessToken_ValidUser_CreatesValidJwt()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin(1);

        // Act
        var token = _tokenService.GenerateAccessToken(usuario);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.NotNull(jwtToken);
    }

    [Fact]
    public void GenerateAccessToken_IncludesUserIdClaim()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin(42);

        // Act
        var token = _tokenService.GenerateAccessToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        Assert.NotNull(userIdClaim);
        Assert.Equal("42", userIdClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_IncludesLoginClaim()
    {
        // Arrange
        var usuario = new UsuarioBuilder()
            .WithLogin("admin_teste")
            .WithPerfil(PerfilUsuario.Administrador)
            .Build();

        // Act
        var token = _tokenService.GenerateAccessToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var loginClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);

        Assert.NotNull(loginClaim);
        Assert.Equal("admin_teste", loginClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_IncludesNomeClaim()
    {
        // Arrange
        var usuario = new UsuarioBuilder()
            .WithNome("João Admin")
            .WithPerfil(PerfilUsuario.Administrador)
            .Build();

        // Act
        var token = _tokenService.GenerateAccessToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var nomeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName);

        Assert.NotNull(nomeClaim);
        Assert.Equal("João Admin", nomeClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_IncludesRoleClaim()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateGerente();

        // Act
        var token = _tokenService.GenerateAccessToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

        Assert.NotNull(roleClaim);
        Assert.Equal("Gerente", roleClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_IncludesPerfilClaim()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateCaixa();

        // Act
        var token = _tokenService.GenerateAccessToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var perfilClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "Perfil");

        Assert.NotNull(perfilClaim);
        Assert.Equal("Caixa", perfilClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_TokenExpiresInConfiguredMinutes()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _tokenService.GenerateAccessToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var afterGeneration = DateTime.UtcNow;

        // Token should expire approximately 60 minutes from now (with tolerance)
        var expectedExpiration = beforeGeneration.AddMinutes(_jwtSettings.ExpirationMinutes);
        Assert.True(jwtToken.ValidTo > beforeGeneration);
        Assert.True(jwtToken.ValidTo <= expectedExpiration.AddSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectIssuer()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin();

        // Act
        var token = _tokenService.GenerateAccessToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
    }

    [Fact]
    public void GenerateAccessToken_HasCorrectAudience()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin();

        // Act
        var token = _tokenService.GenerateAccessToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Contains(_jwtSettings.Audience, jwtToken.Audiences);
    }

    #endregion

    #region GenerateRefreshToken Tests

    [Fact]
    public void GenerateRefreshToken_ValidUser_CreatesValidJwt()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin();

        // Act
        var token = _tokenService.GenerateRefreshToken(usuario);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.NotNull(jwtToken);
    }

    [Fact]
    public void GenerateRefreshToken_IncludesUserIdClaim()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin(99);

        // Act
        var token = _tokenService.GenerateRefreshToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

        Assert.NotNull(userIdClaim);
        Assert.Equal("99", userIdClaim.Value);
    }

    [Fact]
    public void GenerateRefreshToken_IncludesTokenTypeClaimAsRefresh()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin();

        // Act
        var token = _tokenService.GenerateRefreshToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        var tokenTypeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "TokenType");

        Assert.NotNull(tokenTypeClaim);
        Assert.Equal("Refresh", tokenTypeClaim.Value);
    }

    [Fact]
    public void GenerateRefreshToken_ExpiresInConfiguredDays()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin();
        var beforeGeneration = DateTime.UtcNow;

        // Act
        var token = _tokenService.GenerateRefreshToken(usuario);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Token should expire approximately 7 days from now
        var expectedExpiration = beforeGeneration.AddDays(_jwtSettings.RefreshTokenExpirationDays);
        Assert.True(jwtToken.ValidTo > beforeGeneration);
        Assert.True(jwtToken.ValidTo <= expectedExpiration.AddSeconds(5));
    }

    #endregion

    #region ValidateToken Tests

    [Fact]
    public void ValidateToken_ValidToken_ReturnsClaims()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin(5);
        var token = _tokenService.GenerateAccessToken(usuario);

        // Act
        var principal = _tokenService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        Assert.NotNull(userIdClaim);
        Assert.Equal("5", userIdClaim.Value);
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _tokenService.ValidateToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_TamperedToken_ReturnsNull()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin();
        var token = _tokenService.GenerateAccessToken(usuario);
        // Tamper with the token
        var tamperedToken = token.Substring(0, token.Length - 5) + "XXXXX";

        // Act
        var principal = _tokenService.ValidateToken(tamperedToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        // Arrange - Create a token with 0 minute expiration
        var expiredSettings = new JwtSettings
        {
            SecretKey = _jwtSettings.SecretKey,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            ExpirationMinutes = -1, // Already expired
            RefreshTokenExpirationDays = 7,
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };
        var expiredTokenService = new TokenService(Options.Create(expiredSettings));
        var usuario = UsuarioBuilder.CreateAdmin();
        var token = expiredTokenService.GenerateAccessToken(usuario);

        // Act
        var principal = _tokenService.ValidateToken(token);

        // Assert
        Assert.Null(principal);
    }

    #endregion

    #region GetUserIdFromToken Tests

    [Fact]
    public void GetUserIdFromToken_ValidToken_ReturnsUserId()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateAdmin(123);
        var token = _tokenService.GenerateAccessToken(usuario);

        // Act
        var userId = _tokenService.GetUserIdFromToken(token);

        // Assert
        Assert.NotNull(userId);
        Assert.Equal(123, userId);
    }

    [Fact]
    public void GetUserIdFromToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var userId = _tokenService.GetUserIdFromToken(invalidToken);

        // Assert
        Assert.Null(userId);
    }

    #endregion

    #region GetRoleFromToken Tests

    [Fact]
    public void GetRoleFromToken_ValidToken_ReturnsRole()
    {
        // Arrange
        var usuario = UsuarioBuilder.CreateGerente();
        var token = _tokenService.GenerateAccessToken(usuario);

        // Act
        var role = _tokenService.GetRoleFromToken(token);

        // Assert
        Assert.NotNull(role);
        Assert.Equal("Gerente", role);
    }

    [Fact]
    public void GetRoleFromToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var role = _tokenService.GetRoleFromToken(invalidToken);

        // Assert
        Assert.Null(role);
    }

    #endregion
}
