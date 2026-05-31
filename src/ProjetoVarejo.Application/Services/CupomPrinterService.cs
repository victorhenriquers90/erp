// PHASE 2.5 COMPLETED: CupomPrinterService moved to Infrastructure.Services
//
// The implementation lives in:
//   ProjetoVarejo.Infrastructure/Services/CupomPrinterService.cs
//
// It implements ICupomPrinterService from:
//   ProjetoVarejo.Application/Contracts/Services/ICupomPrinterService.cs
//
// Reason for Infrastructure placement: the service depends on ESC/POS printer drivers
// (EscPosPrinter, EscPosBuilder), NFC-e QR-code helpers (QrCodeNfce, ChaveAcessoNfce),
// and ImpressoraConfig — all Infrastructure types.
// Application code references only the ICupomPrinterService abstraction.
