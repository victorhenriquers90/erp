using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class AuditoriaWindow : UserControl
{
    private readonly AuditLogService _auditLogService;
    private readonly IServiceProvider _serviceProvider;

    public AuditoriaWindow(AuditLogService auditLogService, IServiceProvider serviceProvider)
    {
        _auditLogService = auditLogService;
        _serviceProvider = serviceProvider;
        InitializeComponent();
        DtDe.SelectedDate = DateTime.Today.AddDays(-7);
        DtAte.SelectedDate = DateTime.Today;
        TxtEntidade.Text = "AutorizacaoSupervisorPDV";
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var de = DtDe.SelectedDate ?? DateTime.Today.AddDays(-7);
        var ate = (DtAte.SelectedDate ?? DateTime.Today).AddDays(1);
        string? entidade = string.IsNullOrWhiteSpace(TxtEntidade.Text) ? null : TxtEntidade.Text.Trim();

        var logs = await _auditLogService.ListarAsync(de, ate, entidade);

        if (ChkSomenteSupervisorPdv.IsChecked == true)
            logs = logs.Where(l => l.Entidade == "AutorizacaoSupervisorPDV").ToList();

        var filtroUsuario = TxtUsuario.Text.Trim();
        if (!string.IsNullOrWhiteSpace(filtroUsuario))
        {
            logs = logs.Where(l =>
                    (l.Usuario?.Nome?.Contains(filtroUsuario, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (l.Usuario?.Login?.Contains(filtroUsuario, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        DgAuditoria.ItemsSource = logs.Select(log => new AuditoriaLinhaUi(
            log.Data.ToString("dd/MM/yyyy HH:mm:ss"),
            log.Usuario?.Nome ?? "(sistema)",
            log.Entidade,
            log.RegistroId ?? "-",
            log.Tipo.ToString(),
            MontarResumo(log),
            log.ValoresAntes,
            log.ValoresDepois)).ToList();
    }

    private static string MontarResumo(AuditLog log)
    {
        if (log.Entidade != "AutorizacaoSupervisorPDV")
            return "Alteração registrada.";

        try
        {
            var antes = string.IsNullOrWhiteSpace(log.ValoresAntes) ? null : JsonDocument.Parse(log.ValoresAntes);
            var depois = string.IsNullOrWhiteSpace(log.ValoresDepois) ? null : JsonDocument.Parse(log.ValoresDepois);
            var operacao = antes?.RootElement.TryGetProperty("Operacao", out var opEl) == true ? opEl.GetString() : "Operacao";
            var operador = antes?.RootElement.TryGetProperty("OperadorNome", out var operadorEl) == true ? operadorEl.GetString() : "(operador)";
            var supervisor = depois?.RootElement.TryGetProperty("SupervisorNome", out var supervisorEl) == true ? supervisorEl.GetString() : "(supervisor)";

            if (operacao == Permissao.AplicarDesconto.ToString())
            {
                var valor = depois?.RootElement.TryGetProperty("ValorDesconto", out var valorEl) == true
                    ? valorEl.GetDecimal().ToString("C", new CultureInfo("pt-BR"))
                    : "sem valor";
                return $"Supervisor {supervisor} autorizou desconto ({valor}) para operador {operador}.";
            }

            if (operacao == Permissao.CancelarVenda.ToString())
                return $"Supervisor {supervisor} autorizou cancelamento para operador {operador}.";

            return $"Supervisor {supervisor} autorizou operação {operacao} para operador {operador}.";
        }
        catch
        {
            return "Autorização de supervisor no PDV.";
        }
    }

    private async void Filtrar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }

    private void VerDetalhes_Click(object sender, RoutedEventArgs e)
    {
        if (DgAuditoria.SelectedItem is not AuditoriaLinhaUi linha)
        {
            MessageBox.Show("Selecione um item da auditoria.", "Auditoria", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var detalhesWindow = _serviceProvider.GetRequiredService<AuditoriaDetalhesWindow>();
        detalhesWindow.Owner = Window.GetWindow(this);
        detalhesWindow.Carregar(linha);
        detalhesWindow.ShowDialog();
    }

    private void DgAuditoria_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        VerDetalhes_Click(sender, e);
    }
}

public sealed record AuditoriaLinhaUi(
    string Data,
    string Usuario,
    string Entidade,
    string Registro,
    string Tipo,
    string Resumo,
    string? ValoresAntes,
    string? ValoresDepois);
