using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class AdministracaoSistemaViewModel : BaseViewModel
{
    public ObservableCollection<ModuloResumo> Modulos { get; } =
    [
        new("Empresa",       "Dados da loja, matriz, filial e identificacao fiscal.",                         "Base WPF criada", SymbolRegular.Building24),
        new("Servidor local","Conexao com o banco da loja e parametros da rede.",                            "Base WPF criada", SymbolRegular.Server24),
        new("Configuracoes", "Preferencias gerais do sistema, PDV e comportamento operacional.",             "Base WPF criada", SymbolRegular.Settings24),
        new("Modulos",       "Controle de recursos habilitados por perfil de loja.",                         "Base WPF criada", SymbolRegular.Grid24),
        new("Supervisor",    "Liberacoes administrativas e autorizacoes sensiveis.",                         "Base WPF criada", SymbolRegular.Shield24),
        new("Relatorios",    "Parametros de impressao, exportacao e filtros padrao.",                        "Base WPF criada", SymbolRegular.DocumentEdit24),
    ];
}
