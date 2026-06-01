using System.Windows;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class RelatorioDetalhesTecnicosWindow : Window
{
    public RelatorioDetalhesTecnicosWindow()
    {
        InitializeComponent();
    }

    public void Carregar(string abaAtiva, string periodo, string resumo, string csv)
    {
        TxtContexto.Text = "Visualizacao tecnica para conferencia e suporte.";
        TxtAba.Text = abaAtiva;
        TxtPeriodo.Text = periodo;
        TxtResumo.Text = string.IsNullOrWhiteSpace(resumo) ? "(vazio)" : resumo;
        TxtCsv.Text = string.IsNullOrWhiteSpace(csv) ? "(vazio)" : csv;
    }

    private void CopiarResumo_Click(object sender, RoutedEventArgs e)
    {
        CopiarTexto(TxtResumo.Text, "Resumo copiado para a area de transferencia.");
    }

    private void CopiarCsv_Click(object sender, RoutedEventArgs e)
    {
        CopiarTexto(TxtCsv.Text, "CSV copiado para a area de transferencia.");
    }

    private static void CopiarTexto(string texto, string mensagemSucesso)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto == "(vazio)")
        {
            MessageBox.Show("Nao ha conteudo para copiar.", "Relatorios", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetText(texto);
        MessageBox.Show(mensagemSucesso, "Relatorios", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Fechar_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
