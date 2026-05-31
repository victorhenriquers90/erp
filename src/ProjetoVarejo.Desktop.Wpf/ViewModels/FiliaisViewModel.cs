using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class FiliaisViewModel : BaseViewModel
{
    private readonly FilialService _service;

    [ObservableProperty] private ObservableCollection<Filial> _filiais = [];
    [ObservableProperty] private string _total = "0 filiais";
    [ObservableProperty] private string _erro = string.Empty;

    public FiliaisViewModel(FilialService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando filiais...");
        Erro = string.Empty;
        try
        {
            var lista = await _service.ListarAsync();
            Filiais = new ObservableCollection<Filial>(lista);
            Total = $"{lista.Count} filial(is)";
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
