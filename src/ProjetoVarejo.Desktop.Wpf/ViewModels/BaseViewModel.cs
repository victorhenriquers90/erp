using CommunityToolkit.Mvvm.ComponentModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

/// <summary>Base para todos os ViewModels com notificação automática.</summary>
public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = string.Empty;

    protected void SetBusy(bool busy, string msg = "")
    {
        IsBusy = busy;
        StatusMessage = msg;
    }
}
