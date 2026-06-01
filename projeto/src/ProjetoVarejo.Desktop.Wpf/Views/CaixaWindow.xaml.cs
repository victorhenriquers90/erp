using System.Globalization;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class CaixaWindow : Window
{
    private readonly CaixaService _caixaService;
    private readonly CaixaAberturaWindow _aberturaWindow;
    private readonly CaixaMovimentoWindow _movimentoWindow;
    private readonly CaixaFechamentoWindow _fechamentoWindow;
    private readonly IServiceProvider _services;
    private readonly CultureInfo _ptBr = new("pt-BR");
    private CaixaSessao? _caixaAtual;

    public CaixaWindow(
        CaixaService caixaService,
        CaixaAberturaWindow aberturaWindow,
        CaixaMovimentoWindow movimentoWindow,
        CaixaFechamentoWindow fechamentoWindow,
        IServiceProvider services)
    {
        _caixaService = caixaService;
        _aberturaWindow = aberturaWindow;
        _movimentoWindow = movimentoWindow;
        _fechamentoWindow = fechamentoWindow;
        _services = services;
        InitializeComponent();
        Loaded += async (_, _) => await AtualizarAsync();
    }

    private async Task AtualizarAsync()
    {
        _caixaAtual = await _caixaService.ObterCaixaAbertoAsync();

        if (_caixaAtual == null)
        {
            LblStatusTitulo.Text = "Caixa Fechado";
            LblStatusTitulo.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 35, 24));
            LblStatusDetalhe.Text = "Nenhum caixa aberto para este usuário.";

            LblAbertura.Text = "-";
            LblSuprimentos.Text = "-";
            LblSangrias.Text = "-";
            LblVendas.Text = "-";
            LblEsperado.Text = "-";
            DgFormas.ItemsSource = Array.Empty<object>();

            BtnAbrir.IsEnabled = true;
            BtnSuprimento.IsEnabled = false;
            BtnSangria.IsEnabled = false;
            BtnFechar.IsEnabled = false;
            return;
        }

        var resumo = await _caixaService.ResumoAsync(_caixaAtual.Id);
        LblStatusTitulo.Text = "Caixa Aberto";
        LblStatusTitulo.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 118, 70));
        LblStatusDetalhe.Text = $"Aberto em {_caixaAtual.AbertaEm:dd/MM/yyyy HH:mm} • Valor abertura: {_caixaAtual.ValorAbertura.ToString("C", _ptBr)}";

        LblAbertura.Text = resumo.ValorAbertura.ToString("C", _ptBr);
        LblSuprimentos.Text = resumo.TotalSuprimentos.ToString("C", _ptBr);
        LblSangrias.Text = resumo.TotalSangrias.ToString("C", _ptBr);
        LblVendas.Text = resumo.TotalVendas.ToString("C", _ptBr);
        LblEsperado.Text = resumo.SaldoDinheiroEsperado.ToString("C", _ptBr);

        DgFormas.ItemsSource = resumo.VendasPorForma
            .OrderBy(x => x.Key.ToString())
            .Select(x => new FormaLinhaUi(x.Key.ToString(), x.Value.ToString("C", _ptBr)))
            .ToList();

        BtnAbrir.IsEnabled = false;
        BtnSuprimento.IsEnabled = true;
        BtnSangria.IsEnabled = true;
        BtnFechar.IsEnabled = true;
    }

    private async void Abrir_Click(object sender, RoutedEventArgs e)
    {
        if (!_aberturaWindow.Abrir(this, out var valorAbertura)) return;
        var res = await _caixaService.AbrirAsync(valorAbertura);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao abrir caixa.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        await AtualizarAsync();
    }

    private async void Suprimento_Click(object sender, RoutedEventArgs e)
    {
        if (!_movimentoWindow.Abrir(this, "Suprimento", "Informe o valor de suprimento e motivo.", out var valor, out var motivo))
            return;
        var res = await _caixaService.SuprimentoAsync(valor, motivo);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao registrar suprimento.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        await AtualizarAsync();
    }

    private async void Sangria_Click(object sender, RoutedEventArgs e)
    {
        if (!_movimentoWindow.Abrir(this, "Sangria", "Informe o valor de sangria e motivo.", out var valor, out var motivo))
            return;
        var res = await _caixaService.SangriaAsync(valor, motivo);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao registrar sangria.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        await AtualizarAsync();
    }

    private async void Fechar_Click(object sender, RoutedEventArgs e)
    {
        if (_caixaAtual == null) return;

        var resumo = await _caixaService.ResumoAsync(_caixaAtual.Id);
        if (!_fechamentoWindow.Abrir(this, resumo, out var valorInformado, out var observacao))
            return;

        var res = await _caixaService.FecharAsync(_caixaAtual.Id, valorInformado, observacao);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao fechar caixa.", "Caixa", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var diff = res.Valor?.Diferenca ?? 0;
        var msg = diff == 0 ? "Caixa fechado sem divergência."
            : diff > 0 ? $"Caixa fechado com sobra de {diff.ToString("C", _ptBr)}."
            : $"Caixa fechado com falta de {Math.Abs(diff).ToString("C", _ptBr)}.";
        MessageBox.Show(msg, "Caixa", MessageBoxButton.OK, diff == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        await AtualizarAsync();
    }

    private void AbrirPdv_Click(object sender, RoutedEventArgs e)
    {
        using var scope = _services.CreateScope();
        var pdv = scope.ServiceProvider.GetRequiredService<PdvWindow>();
        pdv.Owner = this;
        pdv.ShowDialog();
        _ = AtualizarAsync();
    }
}

public sealed record FormaLinhaUi(string Forma, string Total);
