using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class OperacoesViewModel : BaseViewModel
{
    public ObservableCollection<ModuloResumo> Modulos { get; } =
    [
        new("Fechamento do dia",     "Conferencia de vendas, caixa e formas de pagamento.",               "Base WPF criada", SymbolRegular.CalendarCheckmark24),
        new("Checklist de producao", "Rotina operacional para padaria, cozinha e preparos.",              "Base WPF criada", SymbolRegular.ClipboardTask24),
        new("Backup",                "Rotinas de copia, restauracao e seguranca dos dados.",              "Base WPF criada", SymbolRegular.ArrowSync24),
        new("Implantacao",           "Assistente inicial para colocar uma loja nova em operacao.",        "Base WPF criada", SymbolRegular.Rocket24),
    ];
}
