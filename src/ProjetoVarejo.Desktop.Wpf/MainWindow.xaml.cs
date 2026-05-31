using ProjetoVarejo.Desktop.Wpf.ViewModels;
using System.Windows;

namespace ProjetoVarejo.Desktop.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}
