using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class ProdutosViewModel : BaseViewModel
{
    private readonly ProdutoService _service;

    [ObservableProperty] private ObservableCollection<Produto> _produtos = [];
    [ObservableProperty] private Produto? _produtoSelecionado;
    [ObservableProperty] private string _filtro = string.Empty;
    [ObservableProperty] private string _total = "0 produtos";
    [ObservableProperty] private string _erro = string.Empty;

    public ProdutosViewModel(ProdutoService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando produtos...");
        Erro = string.Empty;
        try
        {
            var lista = await _service.ListarAsync(string.IsNullOrWhiteSpace(Filtro) ? null : Filtro);
            Produtos = new ObservableCollection<Produto>(lista);
            Total = $"{lista.Count} produto(s)";
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
