using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class FornecedoresViewModel : BaseViewModel
{
    private readonly FornecedorService _service;

    [ObservableProperty] private ObservableCollection<Fornecedor> _fornecedores = [];
    [ObservableProperty] private Fornecedor? _fornecedorSelecionado;
    [ObservableProperty] private string _filtro = string.Empty;
    [ObservableProperty] private string _total = "0 fornecedores";
    [ObservableProperty] private string _erro = string.Empty;

    public FornecedoresViewModel(FornecedorService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando fornecedores...");
        Erro = string.Empty;
        try
        {
            var lista = await _service.ListarAsync(string.IsNullOrWhiteSpace(Filtro) ? null : Filtro);
            Fornecedores = new ObservableCollection<Fornecedor>(lista);
            Total = $"{lista.Count} fornecedor(es)";
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
