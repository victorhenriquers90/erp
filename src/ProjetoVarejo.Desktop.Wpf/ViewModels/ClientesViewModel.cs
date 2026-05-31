using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class ClientesViewModel : BaseViewModel
{
    private readonly ClienteService _svc;

    [ObservableProperty] private ObservableCollection<Cliente> _clientes = [];
    [ObservableProperty] private Cliente? _clienteSelecionado;
    [ObservableProperty] private string   _filtro       = string.Empty;
    [ObservableProperty] private string   _total        = "0 clientes";
    [ObservableProperty] private string   _erro         = string.Empty;

    public ClientesViewModel(ClienteService svc)
    {
        _svc = svc;
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando...");
        Erro = string.Empty;
        try
        {
            var lista = await _svc.ListarAsync(string.IsNullOrWhiteSpace(Filtro) ? null : Filtro);
            Clientes = new ObservableCollection<Cliente>(lista);
            Total = $"{lista.Count} cliente(s)";
        }
        catch (Exception ex) { Erro = ex.Message; }
        finally { SetBusy(false); }
    }

    partial void OnFiltroChanged(string value)
    {
        if (CarregarCommand.CanExecute(null))
            _ = CarregarAsync();
    }
}
