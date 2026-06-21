using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace ProjetoVarejo.Web.Services;

/// <summary>
/// Gerencia o estado de autenticação da sessão web via JWT.
/// O token é armazenado no sessionStorage do browser — fecha a aba = desloga.
/// Timeout automático: o token JWT expira após 1 hora (configurado na API).
/// </summary>
public sealed class JwtAuthStateProvider : AuthenticationStateProvider
{
    private const string ChaveToken = "pv_token";
    private const string ChaveNome  = "pv_nome";
    private const string ChavePerfil = "pv_perfil";

    private readonly IJSRuntime _js;
    private readonly ProjetoVarejoApi _api;

    public JwtAuthStateProvider(IJSRuntime js, ProjetoVarejoApi api)
    {
        _js  = js;
        _api = api;
    }

    // ── Estado de autenticação ────────────────────────────────────────────────

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _js.InvokeAsync<string?>("sessionStorage.getItem", ChaveToken);
            if (string.IsNullOrWhiteSpace(token) || TokenExpirado(token))
                return Anonimo();

            _api.DefinirToken(token);
            var claims = ExtrairClaims(token);
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
            return new AuthenticationState(user);
        }
        catch
        {
            return Anonimo();
        }
    }

    // ── Login / logout ────────────────────────────────────────────────────────

    public async Task LoginAsync(string token, string nomeUsuario, string perfil)
    {
        await _js.InvokeVoidAsync("sessionStorage.setItem", ChaveToken, token);
        await _js.InvokeVoidAsync("sessionStorage.setItem", ChaveNome, nomeUsuario);
        await _js.InvokeVoidAsync("sessionStorage.setItem", ChavePerfil, perfil);
        _api.DefinirToken(token);

        var claims = ExtrairClaims(token);
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt"));
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task LogoutAsync()
    {
        await _js.InvokeVoidAsync("sessionStorage.removeItem", ChaveToken);
        await _js.InvokeVoidAsync("sessionStorage.removeItem", ChaveNome);
        await _js.InvokeVoidAsync("sessionStorage.removeItem", ChavePerfil);
        _api.DefinirToken(null);
        NotifyAuthenticationStateChanged(Task.FromResult(Anonimo()));
    }

    public async Task<string?> ObterNomeAsync() =>
        await _js.InvokeAsync<string?>("sessionStorage.getItem", ChaveNome);

    public async Task<string?> ObterPerfilAsync() =>
        await _js.InvokeAsync<string?>("sessionStorage.getItem", ChavePerfil);

    // ── Helpers privados ─────────────────────────────────────────────────────

    private static AuthenticationState Anonimo() =>
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    private static bool TokenExpirado(string jwt)
    {
        try
        {
            var claims = ExtrairClaims(jwt);
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim == null) return false;
            var exp = DateTimeOffset.FromUnixTimeSeconds(long.Parse(expClaim.Value));
            return exp <= DateTimeOffset.UtcNow;
        }
        catch { return true; }
    }

    private static IEnumerable<Claim> ExtrairClaims(string jwt)
    {
        var partes = jwt.Split('.');
        if (partes.Length < 2) return [];

        var payload = partes[1];
        // Base64Url → Base64 padrão
        switch (payload.Length % 4)
        {
            case 2: payload += "=="; break;
            case 3: payload += "="; break;
        }
        payload = payload.Replace('-', '+').Replace('_', '/');

        var jsonBytes = Convert.FromBase64String(payload);
        var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonBytes);
        return dict?.Select(kv => new Claim(kv.Key, kv.Value.ToString())) ?? [];
    }
}
