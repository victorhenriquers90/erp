using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProjetoVarejo.Api.Configuration;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Serilog;

namespace ProjetoVarejo.Api.Services;

/// <summary>
/// JWT token generation and validation service.
/// Uses HMAC SHA256 algorithm (HS256) for token signing.
/// </summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    /// <summary>
    /// Generate a JWT access token with user claims and permissions.
    /// </summary>
    public string GenerateAccessToken(Usuario usuario)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

            // Build claims list with user information
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, usuario.Login),
                new(ClaimTypes.GivenName, usuario.Nome),
                new(ClaimTypes.Role, usuario.Perfil.ToString()),
                new("Perfil", usuario.Perfil.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            Log.Debug("Token JWT gerado para usuário {Usuario}", usuario.Login);

            return tokenString;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao gerar token JWT para usuário {Usuario}", usuario.Login);
            throw;
        }
    }

    /// <summary>
    /// Generate a JWT access token embedding explicit permission claims.
    /// </summary>
    public string GenerateAccessToken(Usuario usuario, IEnumerable<Permissao> permissoes)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new(ClaimTypes.Name, usuario.Login),
                new(ClaimTypes.GivenName, usuario.Nome),
                new(ClaimTypes.Role, usuario.Perfil.ToString()),
                new("Perfil", usuario.Perfil.ToString())
            };

            // Embed each granted permission as a claim
            foreach (var p in permissoes)
                claims.Add(new Claim("Permissao", p.ToString()));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            Log.Debug("Token JWT gerado (com {Count} permissões) para usuário {Usuario}",
                claims.Count(c => c.Type == "Permissao"), usuario.Login);

            return tokenString;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao gerar token JWT com permissões para usuário {Usuario}", usuario.Login);
            throw;
        }
    }

    /// <summary>
    /// Generate a refresh token for long-lived session refresh.
    /// </summary>
    public string GenerateRefreshToken(Usuario usuario)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

            // Refresh token only contains minimal claims (user ID and token type)
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new("TokenType", "Refresh")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            Log.Debug("Refresh token gerado para usuário {Usuario}", usuario.Login);

            return tokenString;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao gerar refresh token para usuário {Usuario}", usuario.Login);
            throw;
        }
    }

    /// <summary>
    /// Validate a JWT token and return the claims principal if valid.
    /// </summary>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.SecretKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = _jwtSettings.ValidateIssuerSigningKey,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = _jwtSettings.ValidateIssuer,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = _jwtSettings.ValidateAudience,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = _jwtSettings.ValidateLifetime,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            Log.Debug("Token JWT validado com sucesso");
            return principal;
        }
        catch (SecurityTokenExpiredException)
        {
            Log.Warning("Token JWT expirado");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            Log.Warning("Erro ao validar token JWT: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro inesperado ao validar token JWT");
            return null;
        }
    }

    /// <summary>
    /// Extract user ID from token claims.
    /// </summary>
    public int? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null) return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Extract role (perfil) from token claims.
    /// </summary>
    public string? GetRoleFromToken(string token)
    {
        var principal = ValidateToken(token);
        return principal?.FindFirst(ClaimTypes.Role)?.Value;
    }
}
