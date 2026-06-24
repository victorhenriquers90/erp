using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

/// <summary>Opção de filtro de tipo de conta. <c>Valor == null</c> significa "Todos".</summary>
public record TipoOpcao(string Nome, TipoConta? Valor);

/// <summary>Opção de filtro de status de conta. <c>Valor == null</c> significa "Todos".</summary>
public record StatusOpcao(string Nome, StatusConta? Valor);

public partial class FinanceiroViewModel : BaseViewModel
{
    private readonly FinanceiroService _service;

    [ObservableProperty] private ObservableCollection<ContaFinanceira> _contas = [];
    [ObservableProperty] private decimal _totalReceber;
    [ObservableProperty] private decimal _totalPagar;
    [ObservableProperty] private decimal _saldoPrevisto;
    [ObservableProperty] private int _movimentacoes;
    [ObservableProperty] private string _erro = string.Empty;

    public ObservableCollection<TipoOpcao> TiposFiltro { get; } =
    [
        new("Todos", null),
        new("A pagar", TipoConta.Pagar),
        new("A receber", TipoConta.Receber),
    ];

    public ObservableCollection<StatusOpcao> StatusFiltro { get; } =
    [
        new("Todos", null),
        new("Em aberto", StatusConta.EmAberto),
        new("Paga", StatusConta.Paga),
        new("Atrasada", StatusConta.Atrasada),
        new("Cancelada", StatusConta.Cancelada),
    ];

    [ObservableProperty] private TipoOpcao _tipoSelecionado;
    [ObservableProperty] private StatusOpcao _statusSelecionado;
    [ObservableProperty] private DateTime _de = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _ate = DateTime.Today.AddDays(30);

    public FinanceiroViewModel(FinanceiroService service)
    {
        _service = service;
        _tipoSelecionado = TiposFiltro[0];
        _statusSelecionado = StatusFiltro[0];
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando financeiro...");
        Erro = string.Empty;
        try
        {
            var de = De.Date;
            var ate = Ate.Date;
            var contas = await _service.ListarAsync(
                tipo: TipoSelecionado?.Valor,
                status: StatusSelecionado?.Valor,
                de: de,
                ate: ate);
            var resumo = await _service.ResumoAsync(de, ate);
            Contas = new ObservableCollection<ContaFinanceira>(contas);
            Movimentacoes = contas.Count;
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
