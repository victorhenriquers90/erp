using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ProjetoVarejo.Api.Services;

/// <summary>
/// Serviço que transmite um beacon UDP na rede local a cada 5 segundos.
/// Permite que os terminais clientes descubram automaticamente o endereço IP
/// deste servidor sem precisar configurá-lo manualmente.
///
/// Protocolo: UDP broadcast na porta 42999
/// Payload:   {"servico":"ProjetoVarejo","versao":"1.0.0","porta":5094}
///
/// Para usar no cliente: escutar UDP na porta 42999 e ler o campo "ip" do sender.
/// </summary>
public sealed class LanBeaconService : BackgroundService
{
    private const int BeaconPort = 42999;
    private const int IntervalSeconds = 5;

    private readonly ILogger<LanBeaconService> _logger;
    private readonly IConfiguration _config;

    public LanBeaconService(ILogger<LanBeaconService> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Determina a porta HTTP a partir da configuração (URL de escuta)
        var porta = 5094;
        var urls = _config["urls"] ?? _config["Urls"] ?? _config["ASPNETCORE_URLS"] ?? "";
        var urlParts = urls.Split(';', StringSplitOptions.RemoveEmptyEntries);
        foreach (var url in urlParts)
        {
            if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && uri.Port > 0)
            {
                porta = uri.Port;
                break;
            }
        }

        var payload = JsonSerializer.Serialize(new
        {
            servico = "ProjetoVarejo",
            versao = "1.0.0",
            porta
        });
        var bytes = Encoding.UTF8.GetBytes(payload);

        _logger.LogInformation("LanBeaconService iniciado — transmitindo na porta UDP {Port} a cada {Interval}s",
            BeaconPort, IntervalSeconds);

        using var udp = new UdpClient();
        udp.EnableBroadcast = true;

        var destino = new IPEndPoint(IPAddress.Broadcast, BeaconPort);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await udp.SendAsync(bytes, bytes.Length, destino);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "LanBeaconService: erro ao transmitir beacon");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(IntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("LanBeaconService encerrado");
    }
}
