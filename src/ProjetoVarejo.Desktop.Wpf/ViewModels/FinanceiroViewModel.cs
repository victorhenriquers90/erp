using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class FinanceiroViewModel : BaseViewModel
{
    private readonly FinanceiroService _service;

    [ObservableProperty] private ObservableCollection<ContaFinanceira> _contas = [];
    [ObservableProperty] private decimal _totalReceber;
    [ObservableProperty] private decimal _totalPagar;
    [ObservableProperty] private decimal _saldoPrevisto;
    [ObservableProperty] private string _erro = string.Empty;

    public FinanceiroViewModel(FinanceiroService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando financeiro...");
        Erro = string.Empty;
        try
        {
            var de = DateTime.Today.AddDays(-30);
            var ate = DateTime.Today.AddDays(30);
            var contas = await _service.ListarAsync(status: StatusConta.EmAberto, de: de, ate: ate);
            var resumo = await _service.ResumoAsync(de, ate);
            Contas = new ObservableCollection<ContaFinanceira>(contas);
            TotalReceber = resumo.totalReceber;
            TotalPagar = resumo.totalPagar;
            SaldoPrevisto = resumo.saldoPrevisto;
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
