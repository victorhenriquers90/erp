using System.Text.Json;

namespace ProjetoVarejo.Application.Services;

/// <summary>
/// Configuração do sistema de alertas automáticos via WhatsApp.
/// Persistida em %APPDATA%\ProjetoERP\alertas.json.
/// </summary>
public class AlertaConfig
{
    private static readonly string _caminho = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ProjetoERP", "alertas.json");

    public bool Ativo { get; set; } = false;
    public string? TelefoneGerente { get; set; }
    public int HoraCaixaDeveAbrir { get; set; } = 9;           // hora (0-23)
    public int IntervaloVerificacaoMinutos { get; set; } = 15;

    public static AlertaConfig Carregar()
    {
        try
        {
            if (File.Exists(_caminho))
            {
                var json = File.ReadAllText(_caminho);
                return JsonSerializer.Deserialize<AlertaConfig>(json) ?? new AlertaConfig();
            }
        }
        catch { /* arquivo corrompido — usa defaults */ }
        return new AlertaConfig();
    }

    public void Salvar()
    {
        try
        {
            var dir = Path.GetDirectoryName(_caminho)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_caminho, json);
        }
        catch { /* silencioso — não crítico */ }
    }
}
