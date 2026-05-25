using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace ProjetoVarejo.Infrastructure.Pix;

/// <summary>
/// Implementação PIX Dinâmico do Banco Inter (https://developers.inter.co/references/pix).
/// Requer:
///   - Aplicação cadastrada no portal do Inter (PJ)
///   - Certificado .pfx fornecido pelo Inter
///   - client_id e client_secret
///   - Chave PIX cadastrada vinculada à conta
/// </summary>
public class InterPixProvider : IPixDinamicoProvider
{
    public class InterConfig
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
        public string CertificadoPfxCaminho { get; set; } = "";
        public string CertificadoPfxSenha { get; set; } = "";
        public string ChavePix { get; set; } = "";  // chave do recebedor cadastrada no Inter
        public bool Sandbox { get; set; } = true;
    }

    private readonly InterConfig _cfg;
    private string? _token;
    private DateTime _tokenExpiraEm = DateTime.MinValue;

    public string Nome => "Banco Inter";

    public InterPixProvider(InterConfig cfg) => _cfg = cfg;

    private string BaseUrl => _cfg.Sandbox
        ? "https://cdpj-sandbox.partners.uatinter.co"
        : "https://cdpj.partners.bancointer.com.br";

    private HttpClient CriarClient()
    {
        if (string.IsNullOrWhiteSpace(_cfg.CertificadoPfxCaminho))
            throw new InvalidOperationException("Certificado Inter não configurado.");
        if (!File.Exists(_cfg.CertificadoPfxCaminho))
            throw new InvalidOperationException("Arquivo .pfx não encontrado.");

        var cert = new X509Certificate2(_cfg.CertificadoPfxCaminho, _cfg.CertificadoPfxSenha);
        var handler = new HttpClientHandler { ClientCertificateOptions = ClientCertificateOption.Manual };
        handler.ClientCertificates.Add(cert);
        return new HttpClient(handler) { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
    }

    private async Task<string> ObterTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(_token) && DateTime.Now < _tokenExpiraEm)
            return _token!;

        using var http = CriarClient();
        var body = new Dictionary<string, string>
        {
            ["client_id"] = _cfg.ClientId,
            ["client_secret"] = _cfg.ClientSecret,
            ["scope"] = "cob.read cob.write pix.read pix.write webhook.read webhook.write",
            ["grant_type"] = "client_credentials"
        };
        var resp = await http.PostAsync("/oauth/v2/token", new FormUrlEncodedContent(body));
        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Falha OAuth Inter: {resp.StatusCode} — {json}");

        using var doc = JsonDocument.Parse(json);
        _token = doc.RootElement.GetProperty("access_token").GetString();
        var expIn = doc.RootElement.GetProperty("expires_in").GetInt32();
        _tokenExpiraEm = DateTime.Now.AddSeconds(expIn - 30);
        return _token!;
    }

    public async Task<PixCobranca> CriarCobrancaAsync(decimal valor, string descricao, int expiraSegundos = 1800)
    {
        try
        {
            var token = await ObterTokenAsync();
            using var http = CriarClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var txid = "PV" + DateTime.Now.ToString("yyMMddHHmmssfff");
            var payload = new
            {
                calendario = new { expiracao = expiraSegundos },
                valor = new { original = valor.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                chave = _cfg.ChavePix,
                solicitacaoPagador = descricao
            };
            var resp = await http.PutAsJsonAsync($"/pix/v2/cob/{txid}", payload);
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Falha criar cobrança: {resp.StatusCode} — {json}");

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new PixCobranca
            {
                TxId = root.GetProperty("txid").GetString() ?? txid,
                Status = PixStatus.Ativo,
                Valor = valor,
                BrCode = root.GetProperty("pixCopiaECola").GetString() ?? "",
                LocationId = root.TryGetProperty("loc", out var loc) && loc.TryGetProperty("id", out var lid) ? lid.ToString() : null,
                ExpiraEm = DateTime.Now.AddSeconds(expiraSegundos)
            };
        }
        catch (Exception ex)
        {
            return new PixCobranca
            {
                Status = PixStatus.Erro,
                Mensagem = ex.Message
            };
        }
    }

    public async Task<PixStatus> ConsultarStatusAsync(string txId)
    {
        try
        {
            var token = await ObterTokenAsync();
            using var http = CriarClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var resp = await http.GetAsync($"/pix/v2/cob/{txId}");
            if (!resp.IsSuccessStatusCode) return PixStatus.Erro;
            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var s = doc.RootElement.GetProperty("status").GetString() ?? "";
            return s switch
            {
                "ATIVA" => PixStatus.Ativo,
                "CONCLUIDA" => PixStatus.Concluido,
                "REMOVIDA_PELO_USUARIO_RECEBEDOR" or "REMOVIDA_PELO_PSP" => PixStatus.Removido,
                _ => PixStatus.Erro
            };
        }
        catch
        {
            return PixStatus.Erro;
        }
    }
}
