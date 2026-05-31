using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class AuditoriaViewModel : BaseViewModel
{
    private readonly AuditLogService _service;

    [ObservableProperty] private ObservableCollection<AuditLog> _registros = [];
    [ObservableProperty] private string _total = "0 registros";
    [ObservableProperty] private string _erro = string.Empty;

    public AuditoriaViewModel(AuditLogService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando auditoria...");
        Erro = string.Empty;
        try
        {
            var lista = await _service.ListarAsync(DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1));
            Registros = new ObservableCollection<AuditLog>(lista);
            Total = $"{lista.Count} registro(s)";
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }
}
