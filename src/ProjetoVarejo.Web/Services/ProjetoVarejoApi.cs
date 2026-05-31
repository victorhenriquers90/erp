using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ProjetoVarejo.Web.Models;
// ApiResponse<T> is defined in Web.Models.ApiModels

namespace ProjetoVarejo.Web.Services;

public sealed class ApiResultado<T>
{
    public T? Dados { get; init; }
    public string? Erro { get; init; }
    public bool Sucesso => string.IsNullOrWhiteSpace(Erro);

    public static ApiResultado<T> Ok(T dados) => new() { Dados = dados };
    public static ApiResultado<T> Falha(string erro) => new() { Erro = erro };
}

public sealed class ProjetoVarejoApi
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public string BaseUrl { get; }

    public ProjetoVarejoApi(HttpClient http, IConfiguration configuration)
    {
        _http = http;

        // Plano B (hospedado): quando Api:BaseUrl não está configurado, usa o
        // BaseAddress que o Program.cs definiu como HostEnvironment.BaseAddress,
        // apontando automaticamente para o servidor correto na rede local.
        var configUrl = configuration["Api:BaseUrl"]?.Trim().TrimEnd('/');
        BaseUrl = string.IsNullOrEmpty(configUrl)
            ? (http.BaseAddress?.ToString().TrimEnd('/') ?? "")
            : configUrl;

        _http.BaseAddress = new Uri(BaseUrl.EndsWith('/') ? BaseUrl : BaseUrl + "/");

        var apiKey = configuration["Api:Key"];
        _http.DefaultRequestHeaders.Remove("x-api-key");
        if (!string.IsNullOrWhiteSpace(apiKey))
            _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
    }

    // ── Auth ─────────────────────────────────────────────────────────────────
    public async Task<ApiResultado<LoginResultado>> LoginAsync(string usuario, string senha)
    {
        try
        {
            var resp = await _http.PostAsJsonAsync("api/auth/login", new { usuario, senha }, _json);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadFromJsonAsync<ApiErrorResponse>(_json);
                return ApiResultado<LoginResultado>.Falha(err?.Message ?? "Credenciais inválidas.");
            }
            var dados = await resp.Content.ReadFromJsonAsync<ApiSuccessResponse<LoginResultado>>(_json);
            return dados?.Data != null
                ? ApiResultado<LoginResultado>.Ok(dados.Data)
                : ApiResultado<LoginResultado>.Falha("Resposta inválida do servidor.");
        }
        catch (Exception ex) { return ApiResultado<LoginResultado>.Falha(MensagemErro(ex)); }
    }

    public Task<ApiResultado<List<ProdutoResumo>>> ProdutosAsync(string? busca = null) =>
        GetListAsync<ProdutoResumo>($"api/produtos/{Query("q", busca)}");

    public Task<ApiResultado<List<ClienteResumo>>> ClientesAsync(string? busca = null) =>
        GetListAsync<ClienteResumo>($"api/clientes/{Query("q", busca)}");

    public Task<ApiResultado<List<EstoqueAlerta>>> EstoqueAbaixoMinimoAsync() =>
        GetListAsync<EstoqueAlerta>("api/estoque/abaixo-minimo");

    public Task<ApiResultado<List<MovimentoEstoque>>> MovimentosEstoqueAsync() =>
        GetListAsync<MovimentoEstoque>("api/estoque/movimentos");

    public Task<ApiResultado<List<VendaDiariaItem>>> VendasPorDiaAsync(DateTime de, DateTime ate) =>
        GetListAsync<VendaDiariaItem>($"api/relatorios/vendas-por-dia?de={Data(de)}&ate={Data(ate)}");

    public Task<ApiResultado<List<ProdutoRankingItem>>> TopProdutosAsync(DateTime de, DateTime ate, int quantidade = 8) =>
        GetListAsync<ProdutoRankingItem>($"api/relatorios/top-produtos?de={Data(de)}&ate={Data(ate)}&n={quantidade}");

    public Task<ApiResultado<List<FornecedorResumo>>> FornecedoresAsync(string? busca = null) =>
        GetListAsync<FornecedorResumo>($"api/fornecedores/{Query("filtro", busca)}");

    public Task<ApiResultado<List<ContaResumo>>> ContasAsync(string? tipo = null, string? status = null) =>
        GetListAsync<ContaResumo>($"api/financeiro/contas?{(tipo != null ? $"tipo={tipo}&" : "")}{(status != null ? $"status={status}" : "")}");

    public async Task<ApiResultado<ResumoFinanceiro>> ResumoFinanceiroAsync()
    {
        try
        {
            var dados = await _http.GetFromJsonAsync<ApiResponse<ResumoFinanceiro>>("api/financeiro/resumo", _json);
            return dados?.Data != null ? ApiResultado<ResumoFinanceiro>.Ok(dados.Data) : ApiResultado<ResumoFinanceiro>.Falha("Sem dados");
        }
        catch (Exception ex) { return ApiResultado<ResumoFinanceiro>.Falha(MensagemErro(ex)); }
    }

    /// <summary>
    /// Define (ou remove) o token JWT que será enviado no header Authorization.
    /// Chamado pelo JwtAuthStateProvider após login / logout.
    /// </summary>
    public void DefinirToken(string? token)
    {
        _http.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrWhiteSpace(token))
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    public async Task<ApiResultado<bool>> HealthAsync()
    {
        try
        {
            using var response = await _http.GetAsync("");
            return response.IsSuccessStatusCode
                ? ApiResultado<bool>.Ok(true)
                : ApiResultado<bool>.Falha($"API retornou {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return ApiResultado<bool>.Falha(MensagemErro(ex));
        }
    }

    private async Task<ApiResultado<List<T>>> GetListAsync<T>(string url)
    {
        try
        {
            var dados = await _http.GetFromJsonAsync<List<T>>(url, _json);
            return ApiResultado<List<T>>.Ok(dados ?? new());
        }
        catch (Exception ex)
        {
            return ApiResultado<List<T>>.Falha(MensagemErro(ex));
        }
    }

    private static string Query(string nome, string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? "" : $"?{nome}={Uri.EscapeDataString(valor)}";

    private static string Data(DateTime data) => data.ToString("yyyy-MM-dd");

    private static string MensagemErro(Exception ex) =>
        ex is HttpRequestException ? "Nao foi possivel conectar na API." : ex.Message;
}
