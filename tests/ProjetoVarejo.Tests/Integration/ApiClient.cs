using System.Net.Http.Json;
using System.Text.Json;
using ProjetoVarejo.Api.Models;

namespace ProjetoVarejo.Tests.Integration;

/// <summary>
/// HttpClient wrapper for API testing with automatic JWT token injection and response parsing.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private string? _jwtToken;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Set JWT token for authenticated requests.
    /// </summary>
    public void SetAuthToken(string jwtToken)
    {
        _jwtToken = jwtToken;
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
    }

    /// <summary>
    /// Clear JWT token for unauthenticated requests.
    /// </summary>
    public void ClearAuthToken()
    {
        _jwtToken = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// GET request returning ApiResponse<T>.
    /// </summary>
    public async Task<ApiResponse<T>?> GetAsync<T>(string path)
    {
        var response = await _httpClient.GetAsync(path);
        return await ParseResponseAsync<T>(response);
    }

    /// <summary>
    /// GET request returning ApiResponse (non-generic).
    /// </summary>
    public async Task<ApiResponse?> GetAsync(string path)
    {
        var response = await _httpClient.GetAsync(path);
        return await ParseResponseAsync(response);
    }

    /// <summary>
    /// POST request returning ApiResponse<T>.
    /// </summary>
    public async Task<ApiResponse<T>?> PostAsync<T>(string path, object? data = null)
    {
        var content = data != null
            ? JsonContent.Create(data)
            : null;
        var response = await _httpClient.PostAsync(path, content);
        return await ParseResponseAsync<T>(response);
    }

    /// <summary>
    /// POST request returning ApiResponse (non-generic).
    /// </summary>
    public async Task<ApiResponse?> PostAsync(string path, object? data = null)
    {
        var content = data != null
            ? JsonContent.Create(data)
            : null;
        var response = await _httpClient.PostAsync(path, content);
        return await ParseResponseAsync(response);
    }

    /// <summary>
    /// PUT request returning ApiResponse<T>.
    /// </summary>
    public async Task<ApiResponse<T>?> PutAsync<T>(string path, object? data = null)
    {
        var content = data != null
            ? JsonContent.Create(data)
            : null;
        var response = await _httpClient.PutAsync(path, content);
        return await ParseResponseAsync<T>(response);
    }

    /// <summary>
    /// PUT request returning ApiResponse (non-generic).
    /// </summary>
    public async Task<ApiResponse?> PutAsync(string path, object? data = null)
    {
        var content = data != null
            ? JsonContent.Create(data)
            : null;
        var response = await _httpClient.PutAsync(path, content);
        return await ParseResponseAsync(response);
    }

    /// <summary>
    /// DELETE request returning ApiResponse.
    /// </summary>
    public async Task<ApiResponse?> DeleteAsync(string path)
    {
        var response = await _httpClient.DeleteAsync(path);
        return await ParseResponseAsync(response);
    }

    /// <summary>
    /// Get raw HttpResponseMessage for advanced assertions.
    /// </summary>
    public async Task<HttpResponseMessage> GetRawAsync(string path)
    {
        return await _httpClient.GetAsync(path);
    }

    /// <summary>
    /// Parse response as ApiResponse<T>.
    /// </summary>
    private async Task<ApiResponse<T>?> ParseResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(content))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Parse response as ApiResponse (non-generic).
    /// </summary>
    private async Task<ApiResponse?> ParseResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrEmpty(content))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
        }
        catch
        {
            return null;
        }
    }
}
