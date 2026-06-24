using Wpf.Ui.Controls;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public sealed record ModuloResumo(
    string Nome,
    string Descricao,
    string Status,
    SymbolRegular Icone);
