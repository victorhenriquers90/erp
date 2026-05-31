using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class VendasViewModel : BaseViewModel
{
    private readonly VendaService _service;

    [ObservableProperty] private ObservableCollection<Venda> _vendas = [];
    [ObservableProperty] private string _total = "0 vendas";
    [ObservableProperty] private string _erro = string.Empty;

    public VendasViewModel(VendaService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando vendas...");
        Erro = string.Empty;
        try
        {
            var lista = await _service.ListarAsync(DateTime.Today.AddDays(-7), DateTime.Today.AddDays(1));
            Vendas = new ObservableCollection<Venda>(lista);
            Total = $"{lista.Count} venda(s) nos ultimos 7 dias";
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
