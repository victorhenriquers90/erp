using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace ProjetoVarejo.Desktop.Wpf;

/// <summary>
/// Tema claro/escuro. Os tokens de cor são DynamicResource, então a troca é ao vivo
/// (sem reiniciar). A preferência é persistida em config-ui.json.
/// </summary>
public static class ThemeManager
{
    private static readonly string Arquivo = Path.Combine(AppContext.BaseDirectory, "config-ui.json");

    public static bool TemaEscuro { get; private set; }

    private static readonly Dictionary<string, string> Claro = new()
    {
        ["BgApp"] = "#F5F6FA", ["BgCard"] = "#FFFFFF", ["BgInput"] = "#FFFFFF",
        ["HeaderGrid"] = "#F0F2F7", ["TextStrong"] = "#0A1428", ["TextSoft"] = "#555A66",
        ["TextPlaceholder"] = "#8B92A3", ["StrokeSoft"] = "#E5E7EB", ["StrokeMedium"] = "#D1D5DB",
        ["BgSelecao"] = "#EAF1FB",
    };

    private static readonly Dictionary<string, string> Escuro = new()
    {
        ["BgApp"] = "#0F1729", ["BgCard"] = "#1B2536", ["BgInput"] = "#232F42",
        ["HeaderGrid"] = "#232F42", ["TextStrong"] = "#E8EEF6", ["TextSoft"] = "#9AA7B8",
        ["TextPlaceholder"] = "#6B7686", ["StrokeSoft"] = "#2E3B4E", ["StrokeMedium"] = "#3C4A60",
        ["BgSelecao"] = "#28415F",
    };

    public static void AplicarTemaSalvo()
    {
        TemaEscuro = LerEscuro();
        Aplicar(TemaEscuro);
    }

    public static void Alternar(bool escuro)
    {
        TemaEscuro = escuro;
        Aplicar(escuro);
        Salvar(escuro);
    }

    private static void Aplicar(bool escuro)
    {
        var paleta = escuro ? Escuro : Claro;
        var res = System.Windows.Application.Current.Resources;
        foreach (var kv in paleta)
        {
            var cor = (Color)ColorConverter.ConvertFromString(kv.Value);
            var brush = new SolidColorBrush(cor);
            brush.Freeze();
            res[kv.Key] = brush;
        }
    }

    private static bool LerEscuro()
    {
        try
        {
            if (File.Exists(Arquivo))
            {
                var cfg = JsonSerializer.Deserialize<ConfigUi>(File.ReadAllText(Arquivo));
                return cfg?.Tema == "escuro";
            }
        }
        catch { /* usa claro como padrão */ }
        return false;
    }

    private static void Salvar(bool escuro)
    {
        try { File.WriteAllText(Arquivo, JsonSerializer.Serialize(new ConfigUi { Tema = escuro ? "escuro" : "claro" })); }
        catch { /* preferência best-effort */ }
    }

    private sealed class ConfigUi { public string Tema { get; set; } = "claro"; }
}
