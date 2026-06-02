using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProjetoVarejo.Infrastructure.Marketplace;

public class MercadoLivreConfig
{
    public string AppId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string RedirectUri { get; set; } = "https://localhost/ml-callback";
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? AccessTokenExpiraEm { get; set; }
    public long? UserId { get; set; }
}

/// <summary>
/// Connector básico Mercado Livre. Foco: OAuth2 + listar pedidos + atualizar estoque.
/// API ref: https://developers.mercadolibre.com.br/
/// </summary>
public class MercadoLivreConnector : IMarketplaceConnector
{
    private const string BaseAuth = "https://auth.mercadolivre.com.br";
    private const string BaseApi = "https://api.mercadolibre.com";

    private readonly MercadoLivreConfig _cfg;
    public string Nome => "Mercado Livre";

    public MercadoLivreConnector(MercadoLivreConfig cfg) => _cfg = cfg;

    public string ObterUrlAutorizacao() =>
        $"{BaseAuth}/authorization?response_type=code&client_id={_cfg.AppId}&redirect_uri={Uri.EscapeDataString(_cfg.RedirectUri)}";

    public async Task<bool> TrocarCodeAsync(string code)
    {
        using var http = new HttpClient { BaseAddress = new Uri(BaseApi) };
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _cfg.AppId,
            ["client_secret"] = _cfg.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = _cfg.RedirectUri
        };
        var resp = await http.PostAsync("/oauth/token", new FormUrlEncodedContent(body));
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) return false;
        using var doc = JsonDocument.Parse(json);
        _cfg.AccessToken = doc.RootElement.GetProperty("access_token").GetString();
        _cfg.RefreshToken = doc.RootElement.GetProperty("refresh_token").GetString();
        var expIn = doc.RootElement.GetProperty("expires_in").GetInt32();
        _cfg.AccessTokenExpiraEm = DateTime.Now.AddSeconds(expIn - 60);
        _cfg.UserId = doc.RootElement.GetProperty("user_id").GetInt64();
        return true;
    }

    private async Task GarantirTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(_cfg.AccessToken))
            throw new InvalidOperationException("Não autorizado. Use ObterUrlAutorizacao() + TrocarCodeAsync().");
        if (_cfg.AccessTokenExpiraEm.HasValue && DateTime.Now < _cfg.AccessTokenExpiraEm.Value) return;

        using var http = new HttpClient { BaseAddress = new Uri(BaseApi) };
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _cfg.AppId,
            ["client_secret"] = _cfg.ClientSecret,
            ["refresh_token"] = _cfg.RefreshToken ?? ""
        };
        var resp = await http.PostAsync("/oauth/token", new FormUrlEncodedContent(body));
        if (!resp.IsSuccessStatusCode) throw new InvalidOperationException("Falha ao renovar token ML.");
        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        _cfg.AccessToken = doc.RootElement.GetProperty("access_token").GetString();
        _cfg.RefreshToken = doc.RootElement.GetProperty("refresh_token").GetString();
        var expIn = doc.RootElement.GetProperty("expires_in").GetInt32();
        _cfg.AccessTokenExpiraEm = DateTime.Now.AddSeconds(expIn - 60);
    }

    private async Task<HttpClient> ClientAutenticadoAsync()
    {
        await GarantirTokenAsync();
        var http = new HttpClient { BaseAddress = new Uri(BaseApi) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.AccessToken);
        return http;
    }

    public async Task<List<PedidoMarketplace>> ListarPedidosRecentesAsync(int dias = 7)
    {
        var lista = new List<PedidoMarketplace>();
        if (_cfg.UserId == null) return lista;
        using var http = await ClientAutenticadoAsync();
        var de = DateTime.UtcNow.AddDays(-dias).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var resp = await http.GetAsync($"/orders/search?seller={_cfg.UserId}&order.date_created.from={de}");
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) return lista;
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("results", out var results)) return lista;

        foreach (var p in results.EnumerateArray())
        {
            lista.Add(new PedidoMarketplace
            {
                IdMarketplace = p.GetProperty("id").ToString(),
                Data = p.TryGetProperty("date_created", out var dc) && DateTime.TryParse(dc.GetString(), out var d) ? d : DateTime.MinValue,
                CompradorNome = p.TryGetProperty("buyer", out var b) && b.TryGetProperty("nickname", out var nk) ? nk.GetString() ?? "" : "",
                Total = p.TryGetProperty("total_amount", out var t) ? t.GetDecimal() : 0,
                Status = p.TryGetProperty("status", out var s) ? s.GetString() ?? "" : ""
            });
        }
        return lista;
    }

    public async Task<bool> AtualizarEstoqueAsync(string itemId, int novoEstoque)
    {
        using var http = await ClientAutenticadoAsync();
        var resp = await http.PutAsJsonAsync($"/items/{itemId}", new { available_quantity = novoEstoque });
        return resp.IsSuccessStatusCode;
    }
}
