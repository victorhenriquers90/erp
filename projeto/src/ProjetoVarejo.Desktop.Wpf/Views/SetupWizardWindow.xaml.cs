using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class SetupWizardWindow : Window
{
    private TipoModo _modoSelecionado = TipoModo.NaoConfigurado;

    public SetupWizardWindow()
    {
        InitializeComponent();
    }

    private void CardServidor_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => SelecionarModo(TipoModo.Servidor);

    private void CardCliente_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        => SelecionarModo(TipoModo.Cliente);

    private void SelecionarModo(TipoModo modo)
    {
        _modoSelecionado = modo;

        // visual dos cards
        CardServidor.Style = modo == TipoModo.Servidor
            ? (Style)FindResource("CardSelected") : (Style)FindResource("Card");
        CardCliente.Style = modo == TipoModo.Cliente
            ? (Style)FindResource("CardSelected") : (Style)FindResource("Card");

        // painel de configuração
        PainelConfig.Visibility = Visibility.Visible;
        ConfigServidor.Visibility = modo == TipoModo.Servidor ? Visibility.Visible : Visibility.Collapsed;
        ConfigCliente.Visibility  = modo == TipoModo.Cliente  ? Visibility.Visible : Visibility.Collapsed;

        TxtStatusConexao.Text = "";
        BtnConfirmar.IsEnabled = modo == TipoModo.Servidor; // cliente requer teste antes
        TxtInfo.Text = modo == TipoModo.Servidor
            ? "Configure a string de conexão e a porta. A API será registrada como serviço Windows."
            : "Digite a URL da API do servidor e clique em Testar conexão para continuar.";
    }

    private async void TestarConexao_Click(object sender, RoutedEventArgs e)
    {
        var url = TxtUrlApi.Text.TrimEnd('/');
        TxtStatusConexao.Text = "Testando...";
        TxtStatusConexao.Foreground = new SolidColorBrush(Colors.Gray);
        BtnConfirmar.IsEnabled = false;

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            var resp = await http.GetAsync($"{url}/");
            if (resp.IsSuccessStatusCode)
            {
                TxtStatusConexao.Text = $"✅ Conectado com sucesso ao servidor!";
                TxtStatusConexao.Foreground = new SolidColorBrush(Color.FromRgb(0x4A, 0xDE, 0x80));
                BtnConfirmar.IsEnabled = true;
            }
            else
            {
                TxtStatusConexao.Text = $"⚠️ Servidor respondeu HTTP {(int)resp.StatusCode}. Verifique a URL.";
                TxtStatusConexao.Foreground = new SolidColorBrush(Color.FromRgb(0xFB, 0xBF, 0x24));
            }
        }
        catch (Exception ex)
        {
            TxtStatusConexao.Text = $"❌ Falha: {ex.Message}\n\nVerifique se o servidor está ligado e acessível na rede.";
            TxtStatusConexao.Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71));
        }
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        var cfg = new ModoSistema { Modo = _modoSelecionado };

        if (_modoSelecionado == TipoModo.Servidor)
        {
            cfg.ConnectionString = TxtConnStr.Text.Trim();
            cfg.NomeServidor = Environment.MachineName;
            cfg.UrlApi = $"http://localhost:{TxtPortaApi.Text.Trim()}";
        }
        else
        {
            cfg.UrlApi = TxtUrlApi.Text.TrimEnd('/');
            cfg.NomeServidor = new Uri(cfg.UrlApi).Host;
        }

        cfg.Salvar();
        DialogResult = true;
        Close();
    }
}
