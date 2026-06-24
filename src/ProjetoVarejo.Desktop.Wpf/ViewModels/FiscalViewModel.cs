using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class FiscalViewModel : BaseViewModel
{
    public ObservableCollection<ModuloResumo> Modulos { get; } =
    [
        new("Notas fiscais",  "Emissao, consulta e acompanhamento de documentos fiscais.",  "Base WPF criada", SymbolRegular.Document24),
        new("Importar NFe",   "Entrada de mercadorias a partir do XML do fornecedor.",        "Base WPF criada", SymbolRegular.ArrowDownload24),
        new("NFC-e",          "Resultado, contingencia e retorno de autorizacao.",            "Base WPF criada", SymbolRegular.Receipt24),
        new("TEF",            "Integracao de cartoes e comprovantes.",                        "Base WPF criada", SymbolRegular.Wallet24),
        new("PIX",            "Cobrancas, QR Code e confirmacao de pagamento.",               "Base WPF criada", SymbolRegular.QrCode24),
    ];
}
