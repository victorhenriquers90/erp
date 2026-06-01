using System.Text.Json;
using System.Windows;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class AuditoriaDetalhesWindow : Window
{
    public AuditoriaDetalhesWindow()
    {
        InitializeComponent();
    }

    public void Carregar(AuditoriaLinhaUi linha)
    {
        TxtData.Text = linha.Data;
        TxtUsuario.Text = linha.Usuario;
        TxtEntidade.Text = linha.Entidade;
        TxtRegistro.Text = linha.Registro;
        TxtTipo.Text = linha.Tipo;
        TxtValoresAntes.Text = FormatarJson(linha.ValoresAntes);
        TxtValoresDepois.Text = FormatarJson(linha.ValoresDepois);
    }

    private static string FormatarJson(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return "(vazio)";

        try
        {
            using var doc = JsonDocument.Parse(texto);
            return JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
        catch
        {
            return texto;
        }
    }

    private void Fechar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CopiarAntes_Click(object sender, RoutedEventArgs e)
    {
        CopiarTexto(TxtValoresAntes.Text, "ValoresAntes copiado para a área de transferência.");
    }

    private void CopiarDepois_Click(object sender, RoutedEventArgs e)
    {
        CopiarTexto(TxtValoresDepois.Text, "ValoresDepois copiado para a área de transferência.");
    }

    private static void CopiarTexto(string texto, string mensagemSucesso)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto == "(vazio)")
        {
            MessageBox.Show("Não há conteúdo para copiar.", "Auditoria", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(texto);
        MessageBox.Show(mensagemSucesso, "Auditoria", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
