using System.Text.Json;

namespace ProjetoVarejo.Tests.Integration;

/// <summary>
/// Extension methods for HttpContent to deserialize JSON responses.
/// Used by integration tests to parse API response bodies.
/// </summary>
public static class HttpContentExtensions
{
    /// <summary>
    /// Read HttpContent as a deserialized object of type T.
    /// Uses case-insensitive JSON deserialization for compatibility.
    /// </summary>
    public static async Task<T> ReadAsAsync<T>(this HttpContent content)
    {
        var json = await content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
}

/// <summary>
/// DTO for login response from /api/auth/login endpoint.
/// Contains authentication tokens and user information.
/// </summary>
public class LoginResponse
{
    public int UsuarioId { get; set; }
    public string? UsuarioNome { get; set; }
    public string? UsuarioPerfil { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public int ExpiresIn { get; set; }
}
