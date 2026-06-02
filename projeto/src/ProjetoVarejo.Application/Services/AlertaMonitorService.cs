using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Infrastructure.WhatsApp;

namespace ProjetoVarejo.Application.Services;

/// <summary>
/// Serviço singleton de monitoramento. Usa IServiceScopeFactory para resolver
/// FilialPainelService (scoped) e WhatsAppService a cada verificação, evitando
/// captive dependency.
/// </summary>
public class AlertaMonitorService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TimeSpan HorarioCaixaDeveAbrirAte { get; set; } = TimeSpan.FromHours(9);
    public string? TelefoneGerente { get; set; }
    public bool AlertasAtivos { get; set; } = false;

    private readonly Dictionary<int, bool> _estadoAnterior = new();
    private readonly HashSet<int> _alertasCaixaEnviados = new();
    private DateTime _ultimaLimpezaDia = DateTime.Today;

    public AlertaMonitorService(IServiceScopeFactory scopeFactory)
        => _scopeFactory = scopeFactory;

    public async Task VerificarAsync()
    {
        if (!AlertasAtivos || string.IsNullOrWhiteSpace(TelefoneGerente)) return;
        LimparAlertasDoDiaSePreciso();

        List<FilialStatus> filiais;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var painelService = scope.ServiceProvider.GetRequiredService<FilialPainelService>();
            filiais = await painelService.ObterStatusTodasAsync();
        }
        catch { return; }

        var agora = DateTime.Now;
        var horaAtual = agora.TimeOfDay;

        foreach (var filial in filiais)
        {
            // Alerta 1: filial ficou offline
            var estavaOnline = _estadoAnterior.TryGetValue(filial.Id, out var anterior) && anterior;
            if (estavaOnline && !filial.Online)
                _ = EnviarAlertaAsync($"⚠️ Filial {NomeFilial(filial)} ficou offline. Verifique a rede. ({agora:HH:mm})");

            // Alerta 2: caixa não aberto no horário
            if (horaAtual >= HorarioCaixaDeveAbrirAte && !filial.CaixaAberto
                && !_alertasCaixaEnviados.Contains(filial.Id))
            {
                _alertasCaixaEnviados.Add(filial.Id);
                var hl = HorarioCaixaDeveAbrirAte.ToString(@"hh\:mm");
                _ = EnviarAlertaAsync($"🏧 Caixa da filial {NomeFilial(filial)} não foi aberto até {hl}. Verifique.");
            }

            _estadoAnterior[filial.Id] = filial.Online;
        }
    }

    private async Task EnviarAlertaAsync(string mensagem)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var whatsApp = scope.ServiceProvider.GetRequiredService<WhatsAppService>();
            await whatsApp.EnviarTextoAsync(TelefoneGerente!, mensagem);
        }
        catch { }
    }

    private static string NomeFilial(FilialStatus f) => f.Apelido ?? f.Nome;

    private void LimparAlertasDoDiaSePreciso()
    {
        if (DateTime.Today > _ultimaLimpezaDia)
        {
            _alertasCaixaEnviados.Clear();
            _ultimaLimpezaDia = DateTime.Today;
        }
    }
}
