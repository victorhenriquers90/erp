using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class CaixaViewModel : BaseViewModel
{
    private readonly CaixaService _service;

    [ObservableProperty] private CaixaSessao? _caixaAberto;
    [ObservableProperty] private decimal _valorAbertura;
    [ObservableProperty] private string _status = "Caixa nao verificado";
    [ObservableProperty] private string _erro = string.Empty;

    public CaixaViewModel(CaixaService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Verificando caixa...");
        Erro = string.Empty;
        try
        {
            CaixaAberto = await _service.ObterCaixaAbertoAsync();
            Status = CaixaAberto == null ? "Nenhum caixa aberto" : $"Caixa aberto desde {CaixaAberto.AbertaEm:dd/MM/yyyy HH:mm}";
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

    [RelayCommand]
    private async Task AbrirAsync()
    {
        SetBusy(true, "Abrindo caixa...");
        Erro = string.Empty;
        try
        {
            var result = await _service.AbrirAsync(ValorAbertura);
            if (!result.Sucesso)
            {
                Erro = result.Erro ?? "Nao foi possivel abrir o caixa.";
                return;
            }

            await CarregarAsync();
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
