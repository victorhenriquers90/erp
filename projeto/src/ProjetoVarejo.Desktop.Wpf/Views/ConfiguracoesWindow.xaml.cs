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

    public ConfiguracoesWindow(SessaoApp sessao, BackupService backupService, DadosDemoService dadosDemoService)
    {
        _sessao = sessao;
        _backupService = backupService;
        _dadosDemoService = dadosDemoService;
        InitializeComponent();

        LblEmpresa.Text = _sessao.EmpresaAtiva?.NomeFantasia
            ?? _sessao.EmpresaAtiva?.RazaoSocial ?? "Não definida";
        LblUsuario.Text = _sessao.UsuarioLogado?.Nome ?? "-";
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

