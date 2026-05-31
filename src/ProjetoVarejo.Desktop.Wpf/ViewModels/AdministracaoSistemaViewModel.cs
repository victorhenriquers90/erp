using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class AdministracaoSistemaViewModel : BaseViewModel
{
    public ObservableCollection<ModuloResumo> Modulos { get; } =
    [
        new("Empresa", "Dados da loja, matriz, filial e identificacao fiscal.", "Base WPF criada", "OfficeBuildingCogOutline"),
        new("Servidor local", "Conexao com o banco da loja e parametros da rede.", "Base WPF criada", "ServerNetworkOutline"),
        new("Configuracoes", "Preferencias gerais do sistema, PDV e comportamento operacional.", "Base WPF criada", "CogOutline"),
        new("Modulos", "Controle de recursos habilitados por perfil de loja.", "Base WPF criada", "ViewModuleOutline"),
        new("Supervisor", "Liberacoes administrativas e autorizacoes sensiveis.", "Base WPF criada", "ShieldAccountOutline"),
        new("Relatorios", "Parametros de impressao, exportacao e filtros padrao.", "Base WPF criada", "FileCogOutline")
    ];
}
