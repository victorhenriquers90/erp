using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Contracts.Services.DTOs;
using ProjetoVarejo.Application.Services;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class RelatoriosViewModel : BaseViewModel
{
    private readonly RelatorioService _service;

    [ObservableProperty] private ObservableCollection<VendaDiariaItem> _vendasPorDia = [];
    [ObservableProperty] private ObservableCollection<ProdutoRankingItem> _topProdutos = [];
    [ObservableProperty] private DateTime _de = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _ate = DateTime.Today.AddDays(1);
    [ObservableProperty] private decimal _totalVendido;
    [ObservableProperty] private string _erro = string.Empty;

    public RelatoriosViewModel(RelatorioService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Gerando relatorios...");
        Erro = string.Empty;
        try
        {
            var vendas = await _service.VendasPorDiaAsync(De, Ate);
            var produtos = await _service.TopProdutosAsync(De, Ate);
            VendasPorDia = new ObservableCollection<VendaDiariaItem>(vendas);
            TopProdutos = new ObservableCollection<ProdutoRankingItem>(produtos);
            TotalVendido = vendas.Sum(v => v.Total);
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
