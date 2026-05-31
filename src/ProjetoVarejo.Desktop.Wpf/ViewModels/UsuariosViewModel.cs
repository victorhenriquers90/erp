using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class UsuariosViewModel : BaseViewModel
{
    private readonly UsuarioService _service;

    [ObservableProperty] private ObservableCollection<Usuario> _usuarios = [];
    [ObservableProperty] private string _filtro = string.Empty;
    [ObservableProperty] private string _total = "0 usuarios";
    [ObservableProperty] private string _erro = string.Empty;

    public UsuariosViewModel(UsuarioService service) => _service = service;

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando usuarios...");
        Erro = string.Empty;
        try
        {
            var lista = await _service.ListarAsync(string.IsNullOrWhiteSpace(Filtro) ? null : Filtro);
            Usuarios = new ObservableCollection<Usuario>(lista);
            Total = $"{lista.Count} usuario(s)";
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
