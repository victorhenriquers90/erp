using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Serilog;

namespace ProjetoVarejo.Desktop.Services;

/// <summary>
/// Cliente que descobre servidores ProjetoVarejo na rede local via beacon UDP.
/// Útil para modo cliente com auto-discovery na rede local.
///
/// Uso:
/// var servers = await BeaconDiscoveryClient.DiscoverServersAsync(timeoutMs: 3000);
/// foreach (var (ip, porta) in servers)
///     Console.WriteLine($"Encontrado: {ip}:{porta}");
/// </summary>
public static class BeaconDiscoveryClient
{
    private const int BeaconPort = 42999;

    /// <summary>
    /// Escuta por beacons na rede local durante o tempo especificado
    /// e retorna lista de (IP, Port) dos servidores descobertos.
    /// </summary>
    public static async Task<List<(string Ip, int Port)>> DiscoverServersAsync(int timeoutMs = 5000)
    {
        var discovered = new Dictionary<string, int>(); // IP -> Port (deduplicar)

        try
        {
            using var udp = new UdpClient();
            udp.Client.ReceiveTimeout = timeoutMs;

            // Bind para escutar broadcasts
            udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            var localEp = new IPEndPoint(IPAddress.Any, BeaconPort);
            udp.Client.Bind(localEp);

            Log.Debug("BeaconDiscoveryClient: escutando em porta {Port} por {Timeout}ms", BeaconPort, timeoutMs);

            var cts = new System.Threading.CancellationTokenSource(timeoutMs);

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await udp.ReceiveAsync();
                    var payload = System.Text.Encoding.UTF8.GetString(result.Buffer);

                    var doc = JsonSerializer.Deserialize<Dictionary<string, object>>(payload);
                    if (doc == null) continue;

                    var ip = result.RemoteEndPoint.Address.ToString();
                    var port = doc.ContainsKey("porta")
                        ? int.Parse(doc["porta"].ToString() ?? "5094")
                        : 5094;

                    if (!discovered.ContainsKey(ip))
                    {
                        discovered[ip] = port;
                        Log.Information("BeaconDiscoveryClient: servidor descoberto em {Ip}:{Port}", ip, port);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "BeaconDiscoveryClient: erro ao processar beacon");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "BeaconDiscoveryClient: erro ao escutar beacons");
        }

        return discovered.Select(kv => (kv.Key, kv.Value)).ToList();
    }

    /// <summary>
    /// Versão síncrona com timeout.
    /// </summary>
    public static List<(string Ip, int Port)> DiscoverServers(int timeoutMs = 5000)
    {
        try
        {
            return DiscoverServersAsync(timeoutMs).GetAwaiter().GetResult();
        }
        catch
        {
            return [];
        }
    }
}
