using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using ProjetoVarejo.Web.Models;

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
        BaseUrl = configuration["Api:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5094";
        var apiKey = configuration["Api:Key"];

        _http.BaseAddress = new Uri(BaseUrl);
        _http.DefaultRequestHeaders.Remove("x-api-key");
        if (!string.IsNullOrWhiteSpace(apiKey))
            _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
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
