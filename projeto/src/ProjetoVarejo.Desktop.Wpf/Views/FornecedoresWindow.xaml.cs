using System.Windows;
using System.Windows.Controls;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class FornecedoresWindow : UserControl
{
    private readonly FornecedorService _fornecedorService;
    private readonly FornecedorEditorWindow _fornecedorEditorWindow;
    private List<Fornecedor> _fornecedores = [];

    public FornecedoresWindow(FornecedorService fornecedorService, FornecedorEditorWindow fornecedorEditorWindow)
    {
        _fornecedorService = fornecedorService;
        _fornecedorEditorWindow = fornecedorEditorWindow;
        InitializeComponent();
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        var filtro = TxtBusca.Text.Trim();
        _fornecedores = await _fornecedorService.ListarAsync(filtro);
        var ativos = _fornecedores.Count(f => f.Ativo);

        DgFornecedores.ItemsSource = _fornecedores.Select(f => new FornecedorLinhaUi(
            f.Id,
            f.RazaoSocial,
            f.NomeFantasia ?? "-",
            f.Cnpj,
            f.Telefone ?? "-",
            f.Email ?? "-",
            string.Join("/", new[] { f.Cidade, f.Uf }.Where(v => !string.IsNullOrWhiteSpace(v))),
            f.Ativo ? "Ativo" : "Inativo")).ToList();

        LblResumo.Text = $"{_fornecedores.Count} fornecedor(es) listados | {ativos} ativo(s)";
    }

    private Fornecedor? ObterSelecionado()
    {
        if (DgFornecedores.SelectedItem is not FornecedorLinhaUi row)
            return null;
        return _fornecedores.FirstOrDefault(f => f.Id == row.Id);
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
        var novo = new Fornecedor { Ativo = true };
        if (_fornecedorEditorWindow.Abrir(Window.GetWindow(this)!, novo))
            await CarregarAsync();
    }

    private async void Editar_Click(object sender, RoutedEventArgs e)
    {
        var fornecedor = ObterSelecionado();
        if (fornecedor == null)
        {
            MessageBox.Show("Selecione um fornecedor para editar.", "Fornecedores", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var clone = new Fornecedor
        {
            Id = fornecedor.Id,
            RazaoSocial = fornecedor.RazaoSocial,
            NomeFantasia = fornecedor.NomeFantasia,
            Cnpj = fornecedor.Cnpj,
            InscricaoEstadual = fornecedor.InscricaoEstadual,
            Email = fornecedor.Email,
            Telefone = fornecedor.Telefone,
            Contato = fornecedor.Contato,
            Cep = fornecedor.Cep,
            Logradouro = fornecedor.Logradouro,
            Numero = fornecedor.Numero,
            Complemento = fornecedor.Complemento,
            Bairro = fornecedor.Bairro,
            Cidade = fornecedor.Cidade,
            Uf = fornecedor.Uf,
            Observacao = fornecedor.Observacao,
            Ativo = fornecedor.Ativo
        };

        if (_fornecedorEditorWindow.Abrir(Window.GetWindow(this)!, clone))
            await CarregarAsync();
    }

    private async void Excluir_Click(object sender, RoutedEventArgs e)
    {
        var fornecedor = ObterSelecionado();
        if (fornecedor == null)
        {
            MessageBox.Show("Selecione um fornecedor para excluir.", "Fornecedores", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var ok = MessageBox.Show($"Confirma excluir o fornecedor \"{fornecedor.RazaoSocial}\"?", "Fornecedores", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (ok != MessageBoxResult.Yes) return;

        var res = await _fornecedorService.ExcluirAsync(fornecedor.Id);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao excluir.", "Fornecedores", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await CarregarAsync();
    }
}

public sealed record FornecedorLinhaUi(
    int Id,
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string Telefone,
    string Email,
    string CidadeUf,
    string Status);
