using System.Windows;
using System.Windows.Controls;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class FiliaisWindow : UserControl
{
    private readonly NfceService _nfceService;
    private readonly EmpresaEditorWindow _empresaEditorWindow;
    private List<EmpresaConfig> _empresas = [];

    public FiliaisWindow(NfceService nfceService, EmpresaEditorWindow empresaEditorWindow)
    {
        _nfceService = nfceService;
        _empresaEditorWindow = empresaEditorWindow;
        InitializeComponent();
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        _empresas = await _nfceService.ListarEmpresasAsync();

        var filtro = TxtBusca.Text.Trim();
        var lista = string.IsNullOrWhiteSpace(filtro)
            ? _empresas
            : _empresas.Where(e =>
                (e.RazaoSocial?.Contains(filtro, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.NomeFantasia?.Contains(filtro, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Cnpj?.Contains(filtro, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

        var dados = lista.Select(e => new EmpresaLinhaUi(
            e.Id,
            e.RazaoSocial,
            string.IsNullOrWhiteSpace(e.NomeFantasia) ? "-" : e.NomeFantasia,
            string.IsNullOrWhiteSpace(e.Cnpj) ? "-" : e.Cnpj,
            string.Join("/", new[] { e.Cidade, e.Uf }.Where(v => !string.IsNullOrWhiteSpace(v))),
            e.AmbienteHomologacao ? "Homologação" : "Produção",
            e.SerieNfce.ToString())).ToList();

        DgEmpresas.ItemsSource = dados;
        LblResumo.Text = $"{lista.Count} empresa(s) listada(s)";
    }

    private EmpresaConfig? ObterSelecionada()
    {
        if (DgEmpresas.SelectedItem is not EmpresaLinhaUi row)
            return null;
        return _empresas.FirstOrDefault(e => e.Id == row.Id);
    }

    private async void TxtBusca_TextChanged(object sender, TextChangedEventArgs e) => await CarregarAsync();

    private async void Atualizar_Click(object sender, RoutedEventArgs e) => await CarregarAsync();

    private async void Novo_Click(object sender, RoutedEventArgs e)
    {
        var nova = new EmpresaConfig { Ativo = true, AmbienteHomologacao = true };
        if (_empresaEditorWindow.Abrir(Window.GetWindow(this)!, nova))
            await CarregarAsync();
    }

    private async void Editar_Click(object sender, RoutedEventArgs e)
    {
        var sel = ObterSelecionada();
        if (sel == null)
        {
            MessageBox.Show("Selecione uma empresa para editar.", "Filiais", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var empresa = await _nfceService.ObterEmpresaPorIdAsync(sel.Id) ?? sel;
        if (_empresaEditorWindow.Abrir(Window.GetWindow(this)!, empresa))
            await CarregarAsync();
    }
}

public sealed record EmpresaLinhaUi(
    int Id,
    string RazaoSocial,
    string NomeFantasia,
    string Cnpj,
    string CidadeUf,
    string Ambiente,
    string Serie);
