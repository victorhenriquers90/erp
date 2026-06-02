using System.IO;
using System.Text.Json;

namespace ProjetoVarejo.Desktop.Wpf;

/// <summary>
/// Modo de operação desta instalação na rede.
/// </summary>
public enum TipoModo
{
    NaoConfigurado = 0,
    Servidor = 1,  // acessa SQL direto, expõe a API como serviço
    Cliente = 2    // fala com a API via HTTP/HTTPS
}

/// <summary>
/// Configuração persistida por máquina em %APPDATA%\ProjetoERP\modo.json.
/// Gerada no primeiro uso pelo SetupWizardWindow.
/// </summary>
public class ModoSistema
{
    private static readonly string _pasta =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProjetoERP");
    private static readonly string _arquivo = Path.Combine(_pasta, "modo.json");

    public TipoModo Modo { get; set; } = TipoModo.NaoConfigurado;

    /// <summary>URL base da API (ex.: http://192.168.1.10:5000). Usado no modo Cliente.</summary>
    public string? UrlApi { get; set; }

    /// <summary>String de conexão SQL. Preenchida pelo wizard no modo Servidor.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Nome/IP do servidor — exibido nos clientes como info de diagnóstico.</summary>
    public string? NomeServidor { get; set; }

    /// <summary>Versão do schema com que este modo foi configurado.</summary>
    public string Versao { get; set; } = "1.0";

    public static ModoSistema Carregar()
    {
        if (!File.Exists(_arquivo))
            return new ModoSistema();
        try
        {
            var json = File.ReadAllText(_arquivo);
            return JsonSerializer.Deserialize<ModoSistema>(json) ?? new ModoSistema();
        }
        catch
        {
            return new ModoSistema();
        }
    }

    public void Salvar()
    {
        Directory.CreateDirectory(_pasta);
        File.WriteAllText(_arquivo, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
    }

    public static void Resetar()
    {
        if (File.Exists(_arquivo)) File.Delete(_arquivo);
    }

    public bool EstaConfigurado => Modo != TipoModo.NaoConfigurado;
    public bool EhServidor => Modo == TipoModo.Servidor;
    public bool EhCliente => Modo == TipoModo.Cliente;
}
