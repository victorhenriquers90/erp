using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ProjetoVarejo.Desktop.Wpf;

/// <summary>
/// Cliente HTTP tipado para comunicação com a ProjetoVarejo.Api.
/// Usado nas instalações no modo Cliente (a API fica no servidor).
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ApiClient(string urlBase, string? apiKey = null)
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri(urlBase.TrimEnd('/') + "/"),
            Timeout = TimeSpan.FromSeconds(30)
        };
        if (!string.IsNullOrWhiteSpace(apiKey))
            _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    // ── Diagnóstico ─────────────────────────────────────────────────────────

    public async Task<bool> PingAsync()
    {
        try
        {
            var resp = await _http.GetAsync("");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<string> VersaoAsync()
    {
        try
        {
            var resp = await _http.GetAsync("");
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("versao").GetString() ?? "?";
        }
        catch { return "Indisponível"; }
    }

    // ── Autenticação ─────────────────────────────────────────────────────────

    public Task<T?> PostAsync<T>(string rota, object corpo) =>
        EnviarAsync<T>(HttpMethod.Post, rota, corpo);

    // ── Produtos ─────────────────────────────────────────────────────────────

    public Task<List<T>?> ListarProdutosAsync<T>(string? busca = null) =>
        GetAsync<List<T>>($"api/produtos{(busca != null ? $"?q={Uri.EscapeDataString(busca)}" : "")}");

    public Task<T?> BuscarProdutoPorBarrasAsync<T>(string codigo) =>
        GetAsync<T>($"api/produtos/barras/{Uri.EscapeDataString(codigo)}");

    // ── Clientes ─────────────────────────────────────────────────────────────

    public Task<List<T>?> ListarClientesAsync<T>(string? busca = null) =>
        GetAsync<List<T>>($"api/clientes{(busca != null ? $"?q={Uri.EscapeDataString(busca)}" : "")}");

    // ── Financeiro ───────────────────────────────────────────────────────────

    public Task<List<T>?> ListarContasAsync<T>(string? tipo = null, string? status = null, DateTime? de = null, DateTime? ate = null)
    {
        var qs = new List<string>();
        if (tipo != null)   qs.Add($"tipo={tipo}");
        if (status != null) qs.Add($"status={status}");
        if (de != null)     qs.Add($"de={de:yyyy-MM-dd}");
        if (ate != null)    qs.Add($"ate={ate:yyyy-MM-dd}");
        var query = qs.Count > 0 ? "?" + string.Join("&", qs) : "";
        return GetAsync<List<T>>($"api/financeiro/contas{query}");
    }

    public Task<T?> ResumoFinanceiroAsync<T>(DateTime de, DateTime ate) =>
        GetAsync<T>($"api/financeiro/resumo?de={de:yyyy-MM-dd}&ate={ate:yyyy-MM-dd}");

    // ── Relatórios ───────────────────────────────────────────────────────────

    public Task<List<T>?> VendasPorDiaAsync<T>(DateTime de, DateTime ate) =>
        GetAsync<List<T>>($"api/relatorios/vendas-por-dia?de={de:yyyy-MM-dd}&ate={ate:yyyy-MM-dd}");

    public Task<List<T>?> CurvaAbcAsync<T>(DateTime de, DateTime ate) =>
        GetAsync<List<T>>($"api/relatorios/curva-abc?de={de:yyyy-MM-dd}&ate={ate:yyyy-MM-dd}");

    public Task<List<T>?> FluxoCaixaAsync<T>(DateTime de, DateTime ate) =>
        GetAsync<List<T>>($"api/relatorios/fluxo-caixa?de={de:yyyy-MM-dd}&ate={ate:yyyy-MM-dd}");

    public async Task<byte[]?> BaixarRelatorioAsync(string rota)
    {
        try
        {
            var resp = await _http.GetAsync(rota);
            return resp.IsSuccessStatusCode ? await resp.Content.ReadAsByteArrayAsync() : null;
        }
        catch { return null; }
    }

    // ── Caixa ────────────────────────────────────────────────────────────────

    public Task<T?> GetCaixaAbertoAsync<T>() => GetAsync<T>("api/caixa/aberto");

    // ── Notificações ─────────────────────────────────────────────────────────

    public Task<T?> EnviarCobrancasVencidasAsync<T>() =>
        EnviarAsync<T>(HttpMethod.Post, "api/notificacoes/cobrancas-vencidas", new { });

    // ── Infraestrutura ───────────────────────────────────────────────────────

    private async Task<T?> GetAsync<T>(string rota)
    {
        try
        {
            var resp = await _http.GetAsync(rota);
            if (!resp.IsSuccessStatusCode) return default;
            return await resp.Content.ReadFromJsonAsync<T>(_json);
        }
        catch { return default; }
    }

    private async Task<T?> EnviarAsync<T>(HttpMethod metodo, string rota, object corpo)
    {
        try
        {
            var req = new HttpRequestMessage(metodo, rota)
            {
                Content = JsonContent.Create(corpo)
            };
            var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return default;
            return await resp.Content.ReadFromJsonAsync<T>(_json);
        }
        catch { return default; }
    }
}
