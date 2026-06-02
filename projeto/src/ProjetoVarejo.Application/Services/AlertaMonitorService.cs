using ProjetoVarejo.Infrastructure.WhatsApp;

namespace ProjetoVarejo.Application.Services;

/// <summary>
/// Serviço de monitoramento automático que verifica o status das filiais
/// e envia alertas via WhatsApp ao gerente quando detecta problemas.
/// Instancie uma única vez e mantenha vivo (ex.: campo estático no App).
/// Chamar <see cref="VerificarAsync"/> periodicamente via timer.
/// </summary>
public class AlertaMonitorService
{
    private readonly FilialPainelService _painelService;
    private readonly WhatsAppService _whatsApp;

    // ── Configuração ──────────────────────────────────────────────────────
    public TimeSpan HorarioCaixaDeveAbrirAte { get; set; } = TimeSpan.FromHours(9);
    public string? TelefoneGerente { get; set; }
    public bool AlertasAtivos { get; set; } = false;

    // ── Estado interno (persiste entre chamadas do timer) ─────────────────
    /// <summary>filialId → estava Online na última verificação</summary>
    private readonly Dictionary<int, bool> _estadoAnterior = new();

    /// <summary>filialIds que já receberam alerta de caixa fechado hoje</summary>
    private readonly HashSet<int> _alertasCaixaEnviados = new();

    /// <summary>Data em que _alertasCaixaEnviados foi zerado pela última vez</summary>
    private DateTime _ultimaLimpezaDia = DateTime.Today;

    // ── Construtor ────────────────────────────────────────────────────────
    public AlertaMonitorService(FilialPainelService painelService, WhatsAppService whatsApp)
    {
        _painelService = painelService;
        _whatsApp = whatsApp;
    }

    // ── Método principal ──────────────────────────────────────────────────
    /// <summary>
    /// Consulta o status de todas as filiais e envia alertas WhatsApp se necessário.
    /// Deve ser chamado pelo timer de fundo a cada N minutos.
    /// </summary>
    public async Task VerificarAsync()
    {
        if (!AlertasAtivos) return;
        if (string.IsNullOrWhiteSpace(TelefoneGerente)) return;

        // Limpa alertas de caixa à meia-noite
        LimparAlertasDoDiaSePreciso();

        List<FilialStatus> filiais;
        try
        {
            filiais = await _painelService.ObterStatusTodasAsync();
        }
        catch
        {
            // Falha silenciosa: não derruba o timer por problema momentâneo de rede/DB
            return;
        }

        var agora = DateTime.Now;
        var horaAtual = agora.TimeOfDay;

        foreach (var filial in filiais)
        {
            // ── Alerta 1: filial ficou offline ──────────────────────────
            var estavaOnline = _estadoAnterior.TryGetValue(filial.Id, out var anterior) && anterior;
            if (estavaOnline && !filial.Online)
            {
                var msg = $"⚠️ Filial {NomeFilial(filial)} ficou offline. Verifique a rede. ({agora:HH:mm})";
                _ = EnviarAlertaAsync(msg);
            }

            // ── Alerta 2: caixa não aberto no horário ───────────────────
            if (horaAtual >= HorarioCaixaDeveAbrirAte
                && !filial.CaixaAberto
                && !_alertasCaixaEnviados.Contains(filial.Id))
            {
                _alertasCaixaEnviados.Add(filial.Id);
                var horaLimite = HorarioCaixaDeveAbrirAte.ToString(@"hh\:mm");
                var msg = $"🏧 Caixa da filial {NomeFilial(filial)} não foi aberto até {horaLimite}. Verifique.";
                _ = EnviarAlertaAsync(msg);
            }

            // ── Atualiza estado anterior ────────────────────────────────
            _estadoAnterior[filial.Id] = filial.Online;
        }
    }

    // ── Auxiliares ────────────────────────────────────────────────────────
    private async Task EnviarAlertaAsync(string mensagem)
    {
        try
        {
            await _whatsApp.EnviarTextoAsync(TelefoneGerente!, mensagem);
        }
        catch { /* alerta best-effort */ }
    }

    private static string NomeFilial(FilialStatus f)
        => f.Apelido ?? f.Nome;

    private void LimparAlertasDoDiaSePreciso()
    {
        if (DateTime.Today > _ultimaLimpezaDia)
        {
            _alertasCaixaEnviados.Clear();
            _ultimaLimpezaDia = DateTime.Today;
        }
    }
}
