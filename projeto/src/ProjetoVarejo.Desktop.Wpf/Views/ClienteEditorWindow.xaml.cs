using System.Globalization;
using System.Windows;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ClienteEditorWindow : Window
{
    private readonly ClienteService _clienteService;
    private Cliente _clienteAtual = new();

    public ClienteEditorWindow(ClienteService clienteService)
    {
        _clienteService = clienteService;
        InitializeComponent();
    }

    public bool Abrir(Window owner, Cliente cliente)
    {
        Owner = owner;
        _clienteAtual = cliente;

        TxtNome.Text = cliente.Nome;
        TxtCpfCnpj.Text = cliente.CpfCnpj ?? string.Empty;
        TxtTelefone.Text = cliente.Telefone ?? string.Empty;
        TxtEmail.Text = cliente.Email ?? string.Empty;
        TxtCep.Text = cliente.Cep ?? string.Empty;
        TxtLogradouro.Text = cliente.Logradouro ?? string.Empty;
        TxtNumero.Text = cliente.Numero ?? string.Empty;
        TxtBairro.Text = cliente.Bairro ?? string.Empty;
        TxtCidade.Text = cliente.Cidade ?? string.Empty;
        TxtUf.Text = cliente.Uf ?? string.Empty;
        TxtLimiteCredito.Text = cliente.LimiteCredito.ToString("N2");
        TxtComplemento.Text = cliente.Complemento ?? string.Empty;
        TxtObservacao.Text = cliente.Observacao ?? string.Empty;
        ChkAtivo.IsChecked = cliente.Ativo;

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
            if (!decimal.TryParse(TxtLimiteCredito.Text.Replace(".", ""), NumberStyles.Any, new CultureInfo("pt-BR"), out var limite))
            {
                limite = 0;
            }

            _clienteAtual.Nome = TxtNome.Text.Trim();
            _clienteAtual.CpfCnpj = TxtCpfCnpj.Text.Trim();
            _clienteAtual.Telefone = TxtTelefone.Text.Trim();
            _clienteAtual.Email = TxtEmail.Text.Trim();
            _clienteAtual.Cep = TxtCep.Text.Trim();
            _clienteAtual.Logradouro = TxtLogradouro.Text.Trim();
            _clienteAtual.Numero = TxtNumero.Text.Trim();
            _clienteAtual.Bairro = TxtBairro.Text.Trim();
            _clienteAtual.Cidade = TxtCidade.Text.Trim();
            _clienteAtual.Uf = TxtUf.Text.Trim().ToUpperInvariant();
            _clienteAtual.LimiteCredito = limite;
            _clienteAtual.Complemento = TxtComplemento.Text.Trim();
            _clienteAtual.Observacao = TxtObservacao.Text.Trim();
            _clienteAtual.Ativo = ChkAtivo.IsChecked == true;

            var result = await _clienteService.SalvarAsync(_clienteAtual);
            if (!result.Sucesso)
            {
                MessageBox.Show(result.Erro ?? "Falha ao salvar.", "Clientes", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar cliente: {ex.Message}", "Clientes", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnSalvar.IsEnabled = true;
            BtnSalvar.Content = "Salvar";
        }
    }
}
