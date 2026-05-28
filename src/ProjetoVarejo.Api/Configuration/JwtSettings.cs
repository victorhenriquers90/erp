namespace ProjetoVarejo.Api.Configuration;

/// <summary>
/// JWT configuration settings from appsettings.json
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing tokens (minimum 32 characters for HS256)
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer identifier
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience identifier
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration time in days
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Validate that the signing key matches the token signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Validate the issuer claim in the token
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Validate the audience claim in the token
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Validate the token expiration time
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;
}
