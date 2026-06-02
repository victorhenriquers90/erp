using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ProjetoVarejo.Infrastructure.WhatsApp;

public class WhatsAppConfig
{
    public string WebhookUrl { get; set; } = "";       // ex: https://api.z-api.io/instances/XXX/token/YYY/send-text
    public string? AuthHeaderName { get; set; }        // ex: "Client-Token", "Authorization", null
    public string? AuthHeaderValue { get; set; }       // ex: "abc123", "Bearer xyz"
    public string PhoneField { get; set; } = "phone";  // nome do campo no JSON
    public string MessageField { get; set; } = "message";
}

public class WhatsAppResultado
{
    public bool Sucesso { get; set; }
    public string? Resposta { get; set; }
    public string? Erro { get; set; }
}

/// <summary>
/// Cliente genérico de WhatsApp. Faz POST JSON ao webhook configurado.
/// Compatível com Z-API, Twilio (com pequeno wrapper), WhatsApp Business API oficial e outros.
/// </summary>
public class WhatsAppService
{
    private readonly WhatsAppConfig _cfg;

    public WhatsAppService(WhatsAppConfig cfg) => _cfg = cfg;

    public async Task<WhatsAppResultado> EnviarTextoAsync(string telefone, string mensagem)
    {
        if (string.IsNullOrWhiteSpace(_cfg.WebhookUrl))
            return new WhatsAppResultado { Sucesso = false, Erro = "Webhook não configurado." };

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            if (!string.IsNullOrWhiteSpace(_cfg.AuthHeaderName))
                http.DefaultRequestHeaders.Add(_cfg.AuthHeaderName, _cfg.AuthHeaderValue ?? "");

            var payload = new Dictionary<string, object>
            {
                [_cfg.PhoneField] = NormalizarTelefone(telefone),
                [_cfg.MessageField] = mensagem
            };
            var resp = await http.PostAsJsonAsync(_cfg.WebhookUrl, payload);
            var body = await resp.Content.ReadAsStringAsync();
            return new WhatsAppResultado
            {
                Sucesso = resp.IsSuccessStatusCode,
                Resposta = body,
                Erro = resp.IsSuccessStatusCode ? null : $"HTTP {(int)resp.StatusCode}: {body}"
            };
        }
        catch (Exception ex)
        {
            return new WhatsAppResultado { Sucesso = false, Erro = ex.Message };
        }
    }

    public Task<WhatsAppResultado> EnviarConfirmacaoVendaAsync(string telefone, string clienteNome, decimal total, string numeroNfce)
    {
        var msg = $"Olá, {clienteNome}!\n\n" +
                  $"Recebemos sua compra de R$ {total:N2}.\n" +
                  $"NFC-e: {numeroNfce}\n\n" +
                  $"Obrigado pela preferência!";
        return EnviarTextoAsync(telefone, msg);
    }

    public Task<WhatsAppResultado> EnviarCobrancaVencidaAsync(string telefone, string clienteNome, decimal valor, DateTime vencimento, string? linkPagamento = null)
    {
        var msg = $"Olá, {clienteNome}.\n\n" +
                  $"Identificamos um boleto em aberto:\n" +
                  $"Valor: R$ {valor:N2}\n" +
                  $"Vencimento: {vencimento:dd/MM/yyyy}\n\n" +
                  (linkPagamento != null ? $"Pague aqui: {linkPagamento}\n\n" : "") +
                  "Em caso de pagamento já efetuado, desconsidere.";
        return EnviarTextoAsync(telefone, msg);
    }

    public Task<WhatsAppResultado> EnviarPixCopiaECola(string telefone, decimal valor, string brCode)
    {
        var msg = $"Pague R$ {valor:N2} via PIX:\n\n" +
                  $"`{brCode}`\n\n" +
                  "Copie e cole no app do seu banco.";
        return EnviarTextoAsync(telefone, msg);
    }

    private static string NormalizarTelefone(string t)
    {
        var digitos = new string(t.Where(char.IsDigit).ToArray());
        if (digitos.Length >= 10 && !digitos.StartsWith("55"))
            digitos = "55" + digitos;
        return digitos;
    }
}
