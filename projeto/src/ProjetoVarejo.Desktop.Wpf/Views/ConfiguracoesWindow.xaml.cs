using System.Windows;
using System.Windows.Controls;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Infrastructure.Backup;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ConfiguracoesWindow : UserControl
{
    private readonly SessaoApp _sessao;
    private readonly BackupService _backupService;
    private readonly DadosDemoService _dadosDemoService;
    private readonly AlertaMonitorService _alertaMonitor;

    public ConfiguracoesWindow(SessaoApp sessao, BackupService backupService,
        DadosDemoService dadosDemoService, AlertaMonitorService alertaMonitor)
    {
        _sessao = sessao;
        _backupService = backupService;
        _dadosDemoService = dadosDemoService;
        _alertaMonitor = alertaMonitor;
        InitializeComponent();

        LblEmpresa.Text = _sessao.EmpresaAtiva?.NomeFantasia
            ?? _sessao.EmpresaAtiva?.RazaoSocial ?? "Não definida";
        LblUsuario.Text = _sessao.UsuarioLogado?.Nome ?? "-";

        CarregarConfigAlertas();
    }

    private void CarregarConfigAlertas()
    {
        var cfg = AlertaConfig.Carregar();
        ChkAlertasAtivos.IsChecked = cfg.Ativo;
        TxtTelefoneGerente.Text = cfg.TelefoneGerente ?? string.Empty;
        TxtHoraCaixa.Text = cfg.HoraCaixaDeveAbrir.ToString();
        TxtIntervalo.Text = cfg.IntervaloVerificacaoMinutos.ToString();
    }

    private async void Backup_Click(object sender, RoutedEventArgs e)
    {
        BtnBackup.IsEnabled = false;
        BtnBackup.Content = "Gerando backup...";
        ResultadoBox.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _backupService.ExecutarAsync();
            if (result.Sucesso)
            {
                LblResultado.Text = $"Backup gerado com sucesso em: {result.Valor}";
                ResultadoBox.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show(result.Erro ?? "Falha ao gerar backup.", "Backup",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gerar backup: {ex.Message}", "Backup",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnBackup.IsEnabled = true;
            BtnBackup.Content = "Fazer backup agora";
        }
    }

    private void TemaClaro_Click(object sender, RoutedEventArgs e) => ThemeManager.Alternar(false);

    private void TemaEscuro_Click(object sender, RoutedEventArgs e) => ThemeManager.Alternar(true);

    private void SalvarAlertas_Click(object sender, RoutedEventArgs e)
    {
        AlertasBox.Visibility = Visibility.Collapsed;
        try
        {
            if (!int.TryParse(TxtHoraCaixa.Text.Trim(), out var hora) || hora < 0 || hora > 23)
            {
                MessageBox.Show("Hora inválida. Informe um valor entre 0 e 23.", "Alertas WhatsApp",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(TxtIntervalo.Text.Trim(), out var intervalo) || intervalo < 1)
            {
                MessageBox.Show("Intervalo inválido. Informe um valor em minutos maior que zero.", "Alertas WhatsApp",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cfg = new AlertaConfig
            {
                Ativo = ChkAlertasAtivos.IsChecked == true,
                TelefoneGerente = TxtTelefoneGerente.Text.Trim(),
                HoraCaixaDeveAbrir = hora,
                IntervaloVerificacaoMinutos = intervalo
            };
            cfg.Salvar();

            // Atualiza o serviço em memória imediatamente
            _alertaMonitor.AlertasAtivos = cfg.Ativo;
            _alertaMonitor.TelefoneGerente = cfg.TelefoneGerente;
            _alertaMonitor.HorarioCaixaDeveAbrirAte = TimeSpan.FromHours(hora);

            LblAlertas.Text = cfg.Ativo
                ? $"Configuração salva. Alertas ativos — verificando a cada {intervalo} min."
                : "Configuração salva. Alertas desativados.";
            AlertasBox.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar configuração de alertas: {ex.Message}", "Alertas WhatsApp",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Demo_Click(object sender, RoutedEventArgs e)
    {
        BtnDemo.IsEnabled = false;
        BtnDemo.Content = "Populando...";
        DemoBox.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _dadosDemoService.PopularAsync();
            if (result.Sucesso)
            {
                LblDemo.Text = result.Valor;
                DemoBox.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show(result.Erro ?? "Falha ao popular dados.", "Dados de demonstração",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao popular dados: {ex.Message}", "Dados de demonstração",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnDemo.IsEnabled = true;
            BtnDemo.Content = "Popular dados de demonstração";
        }
    }
}

