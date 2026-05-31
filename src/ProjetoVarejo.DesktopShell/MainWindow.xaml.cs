using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace ProjetoVarejo.DesktopShell;

public partial class MainWindow : Window
{
    private readonly ShellSettings _settings;
    private Process? _apiProcess;

    public MainWindow()
    {
        InitializeComponent();
        _settings = LoadSettings();
        Loaded += async (_, _) => await StartAsync();
        Closing += (_, _) => StopOwnedApiProcess();
    }

    private static ShellSettings LoadSettings()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(file))
        {
            return new ShellSettings();
        }

        try
        {
            var json = File.ReadAllText(file);
            return JsonSerializer.Deserialize<ShellSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new ShellSettings();
        }
        catch
        {
            return new ShellSettings();
        }
    }

    private async Task StartAsync()
    {
        var appUrl = NormalizeUrl(_settings.Url);
        StatusText.Text = $"Abrindo {appUrl}";

        if (!await IsApiReadyAsync(appUrl))
        {
            if (_settings.StartApi)
            {
                StartApiProcess();
            }

            if (!await WaitUntilReadyAsync(appUrl))
            {
                ShowStartupError(appUrl);
                return;
            }
        }

        await Browser.EnsureCoreWebView2Async();
        Browser.Source = new Uri(appUrl);
        Browser.NavigationCompleted += (_, _) =>
        {
            Browser.Visibility = Visibility.Visible;
            LoadingPanel.Visibility = Visibility.Collapsed;
        };
    }

    private void StartApiProcess()
    {
        var apiPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, _settings.ApiExeRelativePath));
        if (!File.Exists(apiPath))
        {
            StatusText.Text = $"Interface encontrada, mas o servidor local nao foi localizado em: {apiPath}";
            return;
        }

        StatusText.Text = "Iniciando servidor local do ProjetoVarejo...";
        _apiProcess = Process.Start(new ProcessStartInfo
        {
            FileName = apiPath,
            Arguments = "--environment Development --urls http://127.0.0.1:5094",
            WorkingDirectory = Path.GetDirectoryName(apiPath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private async Task<bool> WaitUntilReadyAsync(string appUrl)
    {
        var timeout = TimeSpan.FromSeconds(Math.Max(15, _settings.StartupTimeoutSeconds));
        var startedAt = DateTimeOffset.UtcNow;

        while (DateTimeOffset.UtcNow - startedAt < timeout)
        {
            if (await IsApiReadyAsync(appUrl))
            {
                return true;
            }

            StatusText.Text = "Aguardando o servidor local responder...";
            await Task.Delay(1500);
        }

        return false;
    }

    private static async Task<bool> IsApiReadyAsync(string appUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            using var response = await http.GetAsync(new Uri(new Uri(appUrl), "api/status"));
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void ShowStartupError(string appUrl)
    {
        StatusText.Text =
            "Nao foi possivel abrir o ProjetoVarejo. Verifique se o SQL Server esta ativo e se a porta 5094 esta livre.";

        MessageBox.Show(
            $"Nao foi possivel iniciar o ProjetoVarejo em {appUrl}.\n\n" +
            "Verifique o SQL Server, a conexao com o servidor e tente novamente.",
            "ProjetoVarejo",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void StopOwnedApiProcess()
    {
        try
        {
            if (_apiProcess is { HasExited: false })
            {
                _apiProcess.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // O processo pode ja ter sido finalizado pelo Windows Service.
        }
    }

    private static string NormalizeUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "http://127.0.0.1:5094/";
        }

        return value.EndsWith('/') ? value : value + "/";
    }
}
