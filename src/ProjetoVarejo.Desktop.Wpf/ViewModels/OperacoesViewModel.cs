using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class OperacoesViewModel : BaseViewModel
{
    public ObservableCollection<ModuloResumo> Modulos { get; } =
    [
        new("Fechamento do dia", "Conferencia de vendas, caixa e formas de pagamento.", "Base WPF criada", "CalendarCheckOutline"),
        new("Checklist de producao", "Rotina operacional para padaria, cozinha e preparos.", "Base WPF criada", "ClipboardCheckOutline"),
        new("Backup", "Rotinas de copia, restauracao e seguranca dos dados.", "Base WPF criada", "DatabaseSyncOutline"),
        new("Implantacao", "Assistente inicial para colocar uma loja nova em operacao.", "Base WPF criada", "RocketLaunchOutline")
    ];
}
