using System.IO;

namespace ProjetoVarejo.Desktop.Wpf;

/// <summary>
/// Registro simples de erros em arquivo, para diagnóstico em campo (suporte).
/// Grava em ./logs/erros-yyyyMMdd.log ao lado do executável.
/// </summary>
public static class AppLog
{
    private static readonly object _lock = new();
    private static readonly string PastaLogs = Path.Combine(AppContext.BaseDirectory, "logs");

    public static string CaminhoArquivoHoje =>
        Path.Combine(PastaLogs, $"erros-{DateTime.Now:yyyyMMdd}.log");

    public static void Erro(string origem, Exception ex)
    {
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(PastaLogs);
                var texto =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{origem}] {ex.GetType().Name}: {ex.Message}" +
                    Environment.NewLine + ex + Environment.NewLine +
                    new string('-', 80) + Environment.NewLine;
                File.AppendAllText(CaminhoArquivoHoje, texto);
            }
        }
        catch
        {
            // logger nunca pode derrubar o app
        }
    }
}
