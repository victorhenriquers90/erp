using System.Windows;
using System.Windows.Controls;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class ClientesWindow : UserControl
{
    private readonly ClienteService _clienteService;
    private readonly ClienteEditorWindow _clienteEditorWindow;
    private List<Cliente> _clientes = [];

    public ClientesWindow(ClienteService clienteService, ClienteEditorWindow clienteEditorWindow)
    {
        _clienteService = clienteService;
        _clienteEditorWindow = clienteEditorWindow;
        InitializeComponent();
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var filtro = TxtBusca.Text.Trim();
        _clientes = await _clienteService.ListarAsync(filtro);
        var ativos = _clientes.Count(c => c.Ativo);
        var dados = _clientes.Select(c => new ClienteLinhaUi(
            c.Id,
            c.Nome,
            c.CpfCnpj ?? "-",
            c.Telefone ?? "-",
            c.Email ?? "-",
            string.Join("/", new[] { c.Cidade, c.Uf }.Where(v => !string.IsNullOrWhiteSpace(v))),
            c.Ativo ? "Ativo" : "Inativo")).ToList();

        DgClientes.ItemsSource = dados;
        LblResumo.Text = $"{_clientes.Count} cliente(s) listados | {ativos} ativo(s)";
    }

    private Cliente? ObterSelecionado()
    {
        if (DgClientes.SelectedItem is not ClienteLinhaUi row)
            return null;
        return _clientes.FirstOrDefault(c => c.Id == row.Id);
    }

    private async void TxtBusca_TextChanged(object sender, TextChangedEventArgs e)
    {
        await CarregarAsync();
    }

    private async void Atualizar_Click(object sender, RoutedEventArgs e)
    {
        await CarregarAsync();
    }

    private async void Novo_Click(object sender, RoutedEventArgs e)
    {
        var novo = new Cliente { Ativo = true };
        if (_clienteEditorWindow.Abrir(Window.GetWindow(this)!, novo))
            await CarregarAsync();
    }

    private async void Editar_Click(object sender, RoutedEventArgs e)
    {
        var cliente = ObterSelecionado();
        if (cliente == null)
        {
            MessageBox.Show("Selecione um cliente para editar.", "Clientes", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var clone = new Cliente
        {
            Id = cliente.Id,
            Nome = cliente.Nome,
            CpfCnpj = cliente.CpfCnpj,
            PessoaJuridica = cliente.PessoaJuridica,
            InscricaoEstadual = cliente.InscricaoEstadual,
            Email = cliente.Email,
            Telefone = cliente.Telefone,
            Cep = cliente.Cep,
            Logradouro = cliente.Logradouro,
            Numero = cliente.Numero,
            Complemento = cliente.Complemento,
            Bairro = cliente.Bairro,
            Cidade = cliente.Cidade,
            Uf = cliente.Uf,
            LimiteCredito = cliente.LimiteCredito,
            Observacao = cliente.Observacao,
            Ativo = cliente.Ativo
        };

        if (_clienteEditorWindow.Abrir(Window.GetWindow(this)!, clone))
            await CarregarAsync();
    }

    private async void Excluir_Click(object sender, RoutedEventArgs e)
    {
        var cliente = ObterSelecionado();
        if (cliente == null)
        {
            MessageBox.Show("Selecione um cliente para excluir.", "Clientes", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ok = MessageBox.Show($"Confirma excluir o cliente \"{cliente.Nome}\"?", "Clientes", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (ok != MessageBoxResult.Yes) return;

        var res = await _clienteService.ExcluirAsync(cliente.Id);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao excluir.", "Clientes", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await CarregarAsync();
    }
}

public sealed record ClienteLinhaUi(
    int Id,
    string Nome,
    string CpfCnpj,
    string Telefone,
    string Email,
    string CidadeUf,
    string Status);
