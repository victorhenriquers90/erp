using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class FiscalViewModel : BaseViewModel
{
    public ObservableCollection<ModuloResumo> Modulos { get; } =
    [
        new("Notas fiscais", "Emissao, consulta e acompanhamento de documentos fiscais.", "Base WPF criada", "FileDocumentOutline"),
        new("Importar NFe", "Entrada de mercadorias a partir do XML do fornecedor.", "Base WPF criada", "FileImportOutline"),
        new("NFC-e", "Resultado, contingencia e retorno de autorizacao.", "Base WPF criada", "ReceiptTextCheckOutline"),
        new("TEF", "Integracao de cartoes e comprovantes.", "Base WPF criada", "CreditCardOutline"),
        new("PIX", "Cobrancas, QR Code e confirmacao de pagamento.", "Base WPF criada", "QrcodeScan")
    ];
}
