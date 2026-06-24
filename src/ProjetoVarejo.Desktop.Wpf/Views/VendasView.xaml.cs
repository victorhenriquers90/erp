using System.Windows.Controls;
using System.Windows.Input;
using ProjetoVarejo.Desktop.Wpf.ViewModels;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class VendasView : UserControl
{
    public VendasView() => InitializeComponent();

    private void HistoricoVendasDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is VendasViewModel vm && vm.AbrirVendaCommand.CanExecute(null))
        {
            vm.AbrirVendaCommand.Execute(null);
        }
    }
}
