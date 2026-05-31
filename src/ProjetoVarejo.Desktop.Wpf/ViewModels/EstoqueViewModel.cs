using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class EstoqueViewModel : BaseViewModel
{
    private readonly EstoqueService _service;

    [ObservableProperty] private ObservableCollection<Produto> _alertas = [];
    [ObservableProperty] private ObservableCollection<MovimentoEstoque> _movimentos = [];
    [ObservableProperty] private string _resumo = "0 alertas";
    [ObservableProperty] private string _erro = string.Empty;

    public EstoqueViewModel(EstoqueService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando estoque...");
        Erro = string.Empty;
        try
        {
            var alertas = await _service.ProdutosAbaixoMinimoAsync();
            var movimentos = await _service.ListarMovimentosAsync(de: DateTime.Today.AddDays(-30), ate: DateTime.Today.AddDays(1));
            Alertas = new ObservableCollection<Produto>(alertas);
            Movimentos = new ObservableCollection<MovimentoEstoque>(movimentos);
            Resumo = $"{alertas.Count} alerta(s) | {movimentos.Count} movimento(s)";
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
