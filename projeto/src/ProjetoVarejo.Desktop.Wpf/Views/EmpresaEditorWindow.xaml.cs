using System.Windows;
using Microsoft.Win32;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class EmpresaEditorWindow : Window
{
    private readonly NfceService _nfceService;
    private EmpresaConfig _empresa = new();

    public EmpresaEditorWindow(NfceService nfceService)
    {
        _nfceService = nfceService;
        InitializeComponent();
    }

    public bool Abrir(Window owner, EmpresaConfig empresa)
    {
        Owner = owner;
        _empresa = empresa;

        TxtRazaoSocial.Text = empresa.RazaoSocial;
        TxtNomeFantasia.Text = empresa.NomeFantasia;
        TxtCnpj.Text = empresa.Cnpj;
        TxtIe.Text = empresa.InscricaoEstadual;
        CmbRegime.SelectedIndex = empresa.RegimeTributario switch
        {
            "2" => 1,
            "3" => 2,
            _ => 0
        };
        TxtTelefone.Text = empresa.Telefone;
        TxtEmail.Text = empresa.Email;
        ChkAtivo.IsChecked = empresa.Ativo;

        TxtCep.Text = empresa.Cep;
        TxtLogradouro.Text = empresa.Logradouro;
        TxtNumero.Text = empresa.Numero;
        TxtComplemento.Text = empresa.Complemento ?? string.Empty;
        TxtBairro.Text = empresa.Bairro;
        TxtCidade.Text = empresa.Cidade;
        TxtUf.Text = empresa.Uf;
        TxtCodMun.Text = empresa.CodigoMunicipioIbge;

        ChkHomologacao.IsChecked = empresa.AmbienteHomologacao;
        TxtSerie.Text = empresa.SerieNfce.ToString();
        TxtProxNumero.Text = empresa.ProximoNumeroNfce.ToString();
        TxtCertCaminho.Text = empresa.CertificadoCaminho;
        TxtCertSenha.Password = empresa.CertificadoSenha;
        TxtCscId.Text = empresa.CscId;
        TxtCscToken.Text = empresa.CscToken;

        return ShowDialog() == true;
    }

    private void Procurar_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Selecione o certificado digital A1",
            Filter = "Certificado A1 (*.pfx;*.p12)|*.pfx;*.p12|Todos os arquivos (*.*)|*.*"
        };
        if (dlg.ShowDialog(this) == true)
            TxtCertCaminho.Text = dlg.FileName;
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
            _empresa.RazaoSocial = TxtRazaoSocial.Text.Trim();
            _empresa.NomeFantasia = TxtNomeFantasia.Text.Trim();
            _empresa.Cnpj = TxtCnpj.Text.Trim();
            _empresa.InscricaoEstadual = TxtIe.Text.Trim();
            _empresa.RegimeTributario = CmbRegime.SelectedIndex switch
            {
                1 => "2",
                2 => "3",
                _ => "1"
            };
            _empresa.Telefone = TxtTelefone.Text.Trim();
            _empresa.Email = TxtEmail.Text.Trim();
            _empresa.Ativo = ChkAtivo.IsChecked == true;

            _empresa.Cep = TxtCep.Text.Trim();
            _empresa.Logradouro = TxtLogradouro.Text.Trim();
            _empresa.Numero = TxtNumero.Text.Trim();
            _empresa.Complemento = TxtComplemento.Text.Trim();
            _empresa.Bairro = TxtBairro.Text.Trim();
            _empresa.Cidade = TxtCidade.Text.Trim();
            _empresa.Uf = TxtUf.Text.Trim().ToUpperInvariant();
            _empresa.CodigoMunicipioIbge = TxtCodMun.Text.Trim();

            _empresa.AmbienteHomologacao = ChkHomologacao.IsChecked == true;
            _empresa.SerieNfce = int.TryParse(TxtSerie.Text.Trim(), out var serie) ? serie : 1;
            _empresa.ProximoNumeroNfce = int.TryParse(TxtProxNumero.Text.Trim(), out var num) ? num : 1;
            _empresa.CertificadoCaminho = TxtCertCaminho.Text.Trim();
            _empresa.CertificadoSenha = TxtCertSenha.Password;
            _empresa.CscId = TxtCscId.Text.Trim();
            _empresa.CscToken = TxtCscToken.Text.Trim();

            var result = await _nfceService.SalvarEmpresaAsync(_empresa);
            if (!result.Sucesso)
            {
                MessageBox.Show(result.Erro ?? "Falha ao salvar.", "Empresa", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar empresa: {ex.Message}", "Empresa", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnSalvar.IsEnabled = true;
            BtnSalvar.Content = "Salvar";
        }
    }
}
