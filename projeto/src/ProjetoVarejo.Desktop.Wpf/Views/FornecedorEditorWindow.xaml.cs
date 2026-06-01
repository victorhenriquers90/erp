using System.Windows;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class FornecedorEditorWindow : Window
{
    private readonly FornecedorService _fornecedorService;
    private Fornecedor _fornecedorAtual = new();

    public FornecedorEditorWindow(FornecedorService fornecedorService)
    {
        _fornecedorService = fornecedorService;
        InitializeComponent();
    }

    public bool Abrir(Window owner, Fornecedor fornecedor)
    {
        Owner = owner;
        _fornecedorAtual = fornecedor;

        TxtRazaoSocial.Text = fornecedor.RazaoSocial;
        TxtNomeFantasia.Text = fornecedor.NomeFantasia ?? string.Empty;
        TxtCnpj.Text = fornecedor.Cnpj;
        TxtTelefone.Text = fornecedor.Telefone ?? string.Empty;
        TxtEmail.Text = fornecedor.Email ?? string.Empty;
        TxtContato.Text = fornecedor.Contato ?? string.Empty;
        TxtCidade.Text = fornecedor.Cidade ?? string.Empty;
        TxtUf.Text = fornecedor.Uf ?? string.Empty;
        TxtObservacao.Text = fornecedor.Observacao ?? string.Empty;
        ChkAtivo.IsChecked = fornecedor.Ativo;

        return ShowDialog() == true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void Salvar_Click(object sender, RoutedEventArgs e)
    {
        BtnSalvar.IsEnabled = false;
        BtnSalvar.Content = "Salvando...";
        try
        {
            _fornecedorAtual.RazaoSocial = TxtRazaoSocial.Text.Trim();
            _fornecedorAtual.NomeFantasia = TxtNomeFantasia.Text.Trim();
            _fornecedorAtual.Cnpj = TxtCnpj.Text.Trim();
            _fornecedorAtual.Telefone = TxtTelefone.Text.Trim();
            _fornecedorAtual.Email = TxtEmail.Text.Trim();
            _fornecedorAtual.Contato = TxtContato.Text.Trim();
            _fornecedorAtual.Cidade = TxtCidade.Text.Trim();
            _fornecedorAtual.Uf = TxtUf.Text.Trim().ToUpperInvariant();
            _fornecedorAtual.Observacao = TxtObservacao.Text.Trim();
            _fornecedorAtual.Ativo = ChkAtivo.IsChecked == true;

            var result = await _fornecedorService.SalvarAsync(_fornecedorAtual);
            if (!result.Sucesso)
            {
                MessageBox.Show(result.Erro ?? "Falha ao salvar.", "Fornecedores", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar fornecedor: {ex.Message}", "Fornecedores", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnSalvar.IsEnabled = true;
            BtnSalvar.Content = "Salvar";
        }
    }
}
