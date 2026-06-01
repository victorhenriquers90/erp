using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class PdvWindow : Window
{
    private readonly VendaService _vendaService;
    private readonly ProdutoService _produtoService;
    private readonly NfceService _nfceService;
    private readonly CaixaService _caixaService;
    private readonly PermissaoService _permissaoService;
    private readonly AuditLogService _auditLogService;
    private readonly SessaoApp _sessao;
    private readonly CupomPrinterService _printer;
    private readonly IServiceProvider _services;
    private readonly CultureInfo _ptBr = new("pt-BR");

    private Venda? _vendaAtual;
    private readonly List<ItemVendaLinhaUi> _itens = [];
    private readonly List<ProdutoSugestaoItem> _sugestoes = [];
    private int _sugestaoSeq;
    private bool _suspendSugestoes;
    private TextBox _campoNumpad = null!;

    public PdvWindow(
        VendaService vendaService,
        ProdutoService produtoService,
        NfceService nfceService,
        CaixaService caixaService,
        PermissaoService permissaoService,
        AuditLogService auditLogService,
        SessaoApp sessao,
        CupomPrinterService printer,
        IServiceProvider services)
    {
        _vendaService = vendaService;
        _produtoService = produtoService;
        _nfceService = nfceService;
        _caixaService = caixaService;
        _permissaoService = permissaoService;
        _auditLogService = auditLogService;
        _sessao = sessao;
        _printer = printer;
        _services = services;

        InitializeComponent();
        PreviewKeyDown += PdvWindow_PreviewKeyDown;
        Loaded += async (_, _) =>
        {
            _campoNumpad = TxtCodigo;
            AplicarModoOperador(false);
            await IniciarNovaVendaAsync();
        };
    }

    private async void PdvWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F4) { await AplicarDescontoAsync(); e.Handled = true; return; }
        if (e.Key == Key.F10) { await FinalizarAsync(); e.Handled = true; return; }
        if (e.Key == Key.F12) { await IniciarNovaVendaAsync(); e.Handled = true; return; }
        if (e.Key == Key.Delete) { await RemoverItemAsync(); e.Handled = true; return; }
        if (e.Key == Key.Escape) { await CancelarVendaAsync(); e.Handled = true; return; }
        if (e.Key == Key.F2) { await BuscarProdutoAsync(); e.Handled = true; return; }
    }

    private async Task IniciarNovaVendaAsync()
    {
        if (!await _permissaoService.TemPermissaoAsync(Permissao.AbrirPdv))
        {
            MessageBox.Show("Seu usuário não possui permissão para abrir o PDV.", "PDV", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        var caixa = await _caixaService.ObterCaixaAbertoAsync();
        if (caixa == null)
        {
            MessageBox.Show("Abra o caixa antes de iniciar vendas.", "PDV", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }

        if (_vendaAtual is { Status: StatusVenda.EmAberto, Itens.Count: > 0 })
        {
            var cancelar = MessageBox.Show(
                "Existe venda em aberto. Deseja cancelar e iniciar nova?",
                "PDV", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (cancelar != MessageBoxResult.Yes)
                return;

            await _vendaService.CancelarAsync(_vendaAtual.Id, "Cancelada para iniciar nova");
        }

        var res = await _vendaService.NovaVendaAsync();
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao iniciar venda.", "PDV", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _vendaAtual = res.Valor;
        LblNumeroVenda.Text = $"Venda: {_vendaAtual!.Numero}";
        _itens.Clear();
        DgItens.ItemsSource = null;
        TxtCodigo.Text = string.Empty;
        TxtQuantidade.Text = "1";
        EsconderSugestoes();
        AtualizarTotais();
        TxtCodigo.Focus();
    }

    private async Task AdicionarItemAsync()
    {
        if (_vendaAtual == null)
            await IniciarNovaVendaAsync();
        if (_vendaAtual == null) return;

        var codigo = TxtCodigo.Text.Trim();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            TxtCodigo.Focus();
            return;
        }

        if (!decimal.TryParse(TxtQuantidade.Text, NumberStyles.Any, _ptBr, out var qtd) || qtd <= 0)
        {
            MessageBox.Show("Quantidade inválida.", "PDV", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtQuantidade.Focus();
            TxtQuantidade.SelectAll();
            return;
        }

        var produto = await _produtoService.BuscarPorCodigoAsync(codigo);
        if (produto == null)
        {
            MessageBox.Show($"Produto '{codigo}' não encontrado.", "PDV", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtCodigo.Focus();
            TxtCodigo.SelectAll();
            return;
        }

        var res = await _vendaService.AdicionarItemAsync(_vendaAtual.Id, produto.Id, qtd);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao adicionar item.", "PDV", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        await RecarregarItensAsync();
        TxtCodigo.Text = string.Empty;
        TxtQuantidade.Text = "1";
        EsconderSugestoes();
        TxtCodigo.Focus();
    }

    private Task BuscarProdutoAsync()
    {
        using var scope = _services.CreateScope();
        var busca = scope.ServiceProvider.GetRequiredService<ProdutoBuscaWindow>();
        if (!busca.Abrir(this, out var produto) || produto == null)
            return Task.CompletedTask;
        TxtCodigo.Text = produto.Codigo;
        TxtCodigo.Focus();
        TxtCodigo.SelectAll();
        EsconderSugestoes();
        return Task.CompletedTask;
    }

    private async Task RecarregarItensAsync()
    {
        if (_vendaAtual == null) return;
        var venda = await _vendaService.BuscarAsync(_vendaAtual.Id);
        if (venda == null) return;

        _vendaAtual = venda;
        _itens.Clear();
        _itens.AddRange(venda.Itens.Select(i => new ItemVendaLinhaUi(
            i.Id,
            i.Produto.Codigo,
            i.Produto.Descricao,
            i.Quantidade.ToString("N3", _ptBr),
            i.PrecoUnitario.ToString("C", _ptBr),
            i.Total.ToString("C", _ptBr))));
        DgItens.ItemsSource = null;
        DgItens.ItemsSource = _itens;
        AtualizarTotais();
    }

    private void AtualizarTotais()
    {
        LblItens.Text = $"Itens: {_vendaAtual?.Itens.Count ?? 0}";
        LblSubtotal.Text = $"Subtotal: {(_vendaAtual?.SubTotal ?? 0).ToString("C", _ptBr)}";
        LblDesconto.Text = $"Desconto: {(_vendaAtual?.Desconto ?? 0).ToString("C", _ptBr)}";
        LblTotal.Text = (_vendaAtual?.Total ?? 0).ToString("C", _ptBr);
    }

    private async Task RemoverItemAsync()
    {
        if (DgItens.SelectedItem is not ItemVendaLinhaUi row)
            return;

        var res = await _vendaService.RemoverItemAsync(row.Id);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao remover item.", "PDV", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        await RecarregarItensAsync();
    }

    private async Task AplicarDescontoAsync()
    {
        if (_vendaAtual == null || _vendaAtual.SubTotal <= 0) return;
        var (podeProsseguir, supervisor) = await GarantirPermissaoOuSupervisorAsync(
                Permissao.AplicarDesconto,
                "Desconto exige autorização",
                "Seu perfil não pode aplicar desconto diretamente. Solicite credenciais de supervisor.");
        if (!podeProsseguir)
            return;

        using var scope = _services.CreateScope();
        var prompt = scope.ServiceProvider.GetRequiredService<ValorPromptWindow>();
        if (!prompt.Abrir(this, "Aplicar desconto", "Valor do desconto (R$)", _vendaAtual.Desconto, out var desconto))
            return;

        var res = await _vendaService.AplicarDescontoAsync(_vendaAtual.Id, desconto);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao aplicar desconto.", "PDV", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (supervisor != null)
            await RegistrarAutorizacaoSupervisorAsync(supervisor, Permissao.AplicarDesconto, desconto);

        await RecarregarItensAsync();
    }

    private async Task CancelarVendaAsync()
    {
        if (_vendaAtual == null || _vendaAtual.Status != StatusVenda.EmAberto) return;
        var (podeProsseguir, supervisor) = await GarantirPermissaoOuSupervisorAsync(
                Permissao.CancelarVenda,
                "Cancelamento exige autorização",
                "Seu perfil não pode cancelar venda diretamente. Solicite credenciais de supervisor.");
        if (!podeProsseguir)
            return;

        if (_vendaAtual.Itens.Count > 0)
        {
            var ok = MessageBox.Show("Cancelar venda atual?", "PDV", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ok != MessageBoxResult.Yes) return;
        }

        var res = await _vendaService.CancelarAsync(_vendaAtual.Id, "Cancelada pelo operador");
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao cancelar venda.", "PDV", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (supervisor != null)
            await RegistrarAutorizacaoSupervisorAsync(supervisor, Permissao.CancelarVenda, null);

        await IniciarNovaVendaAsync();
    }

    private async Task<(bool Autorizado, Usuario? Supervisor)> GarantirPermissaoOuSupervisorAsync(Permissao permissao, string titulo, string mensagem)
    {
        if (await _permissaoService.TemPermissaoAsync(permissao))
            return (true, null);

        using var scope = _services.CreateScope();
        var authWin = scope.ServiceProvider.GetRequiredService<SupervisorAutorizacaoWindow>();
        if (!authWin.Abrir(this, titulo, mensagem, out var login, out var senha))
            return (false, null);

        var auth = await _permissaoService.AutorizarSupervisorAsync(login, senha, permissao);
        if (!auth.Sucesso)
        {
            MessageBox.Show(auth.Erro ?? "Supervisor não autorizado.", "PDV", MessageBoxButton.OK, MessageBoxImage.Warning);
            return (false, null);
        }

        MessageBox.Show($"Operação autorizada por {auth.Valor!.Nome}.", "PDV", MessageBoxButton.OK, MessageBoxImage.Information);
        return (true, auth.Valor);
    }

    private async Task RegistrarAutorizacaoSupervisorAsync(Usuario supervisor, Permissao operacao, decimal? valorDesconto)
    {
        var antesObj = new
        {
            OperadorId = _sessao.UsuarioLogado?.Id,
            OperadorLogin = _sessao.UsuarioLogado?.Login,
            OperadorNome = _sessao.UsuarioLogado?.Nome,
            Operacao = operacao.ToString(),
            VendaId = _vendaAtual?.Id,
            VendaNumero = _vendaAtual?.Numero
        };

        var depoisObj = new
        {
            SupervisorId = supervisor.Id,
            SupervisorLogin = supervisor.Login,
            SupervisorNome = supervisor.Nome,
            Momento = DateTime.Now,
            ValorDesconto = valorDesconto
        };

        await _auditLogService.RegistrarComUsuarioAsync(
            supervisor.Id,
            "AutorizacaoSupervisorPDV",
            (_vendaAtual?.Id ?? 0).ToString(),
            TipoAuditoria.Update,
            JsonSerializer.Serialize(antesObj),
            JsonSerializer.Serialize(depoisObj));
    }

    private async Task FinalizarAsync()
    {
        if (_vendaAtual == null || !_vendaAtual.Itens.Any())
        {
            MessageBox.Show("Adicione itens antes de finalizar.", "PDV", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        using var scope = _services.CreateScope();
        var pagamentoWin = scope.ServiceProvider.GetRequiredService<PagamentoVendaWindow>();
        if (!pagamentoWin.Abrir(this, _vendaAtual.Total, out var pagamentos))
            return;

        var res = await _vendaService.FinalizarAsync(_vendaAtual.Id, pagamentos);
        if (!res.Sucesso)
        {
            MessageBox.Show(res.Erro ?? "Falha ao finalizar venda.", "PDV", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var venda = res.Valor!;
        var caixaAberto = await _caixaService.ObterCaixaAbertoAsync();
        if (caixaAberto != null)
            await _caixaService.RegistrarVendaAsync(caixaAberto.Id, venda.Id, pagamentos);

        var emitir = MessageBox.Show(
            $"Venda finalizada!\n\nTotal: {venda.Total.ToString("C", _ptBr)}\nPago: {venda.ValorPago.ToString("C", _ptBr)}\nTroco: {venda.Troco.ToString("C", _ptBr)}\n\nEmitir NFC-e agora?",
            "PDV", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (emitir == MessageBoxResult.Yes)
        {
            await EmitirNfceAsync(venda.Id);
        }
        else
        {
            await TentarImpressaoNaoFiscalAsync(venda.Id, null);
        }

        await IniciarNovaVendaAsync();
    }

    private async Task EmitirNfceAsync(int vendaId)
    {
        try
        {
            bool contingencia = false;
            if (!await _nfceService.SefazOnlineAsync())
            {
                var result = MessageBox.Show(
                    "SEFAZ aparentemente offline.\nEmitir em contingência (tpEmis=9)?",
                    "NFC-e", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel) return;
                contingencia = result == MessageBoxResult.Yes;
            }

            var res = contingencia
                ? await _nfceService.EmitirContingenciaAsync(vendaId)
                : await _nfceService.EmitirAsync(vendaId);

            if (!res.Sucesso)
            {
                MessageBox.Show("Falha NFC-e: " + res.Erro, "NFC-e", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var nota = res.Valor!;
            var msg = $"NFC-e processada.\nStatus: {nota.Status}\nMensagem: {nota.MensagemSefaz}";
            MessageBox.Show(msg, "NFC-e", MessageBoxButton.OK, MessageBoxImage.Information);
            await TentarImpressaoNaoFiscalAsync(vendaId, nota);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Erro ao emitir NFC-e: " + ex.Message, "NFC-e", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task TentarImpressaoNaoFiscalAsync(int vendaId, NotaFiscal? nota)
    {
        var empresa = await _nfceService.ObterEmpresaAsync();
        var venda = await _vendaService.BuscarAsync(vendaId);
        if (empresa == null || venda == null) return;
        if (!empresa.ImprimirAutomatico || string.IsNullOrWhiteSpace(empresa.ImpressoraDestino)) return;

        var resPrint = await _printer.ImprimirVendaAsync(venda, empresa, nota);
        if (!resPrint.Sucesso)
            MessageBox.Show("Aviso na impressão: " + resPrint.Erro, "PDV", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private async void Adicionar_Click(object sender, RoutedEventArgs e)
    {
        await AdicionarItemAsync();
    }

    private async void Buscar_Click(object sender, RoutedEventArgs e)
    {
        await BuscarProdutoAsync();
    }

    private void AumentarQtd_Click(object sender, RoutedEventArgs e)
    {
        var qtd = LerQuantidade();
        qtd += 1;
        TxtQuantidade.Text = qtd.ToString("N0", _ptBr);
    }

    private void DiminuirQtd_Click(object sender, RoutedEventArgs e)
    {
        var qtd = LerQuantidade();
        qtd = Math.Max(1, qtd - 1);
        TxtQuantidade.Text = qtd.ToString("N0", _ptBr);
    }

    private void QtdPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button btn) return;
        TxtQuantidade.Text = btn.Content?.ToString() ?? "1";
        TxtQuantidade.Focus();
        TxtQuantidade.SelectAll();
    }

    private decimal LerQuantidade()
    {
        if (decimal.TryParse(TxtQuantidade.Text, NumberStyles.Any, _ptBr, out var qtd) && qtd > 0)
            return decimal.Floor(qtd);
        return 1;
    }

    private async Task AtualizarSugestoesAsync()
    {
        if (_suspendSugestoes)
            return;

        var texto = TxtCodigo.Text.Trim();
        if (texto.Length < 2)
        {
            EsconderSugestoes();
            return;
        }

        var seq = ++_sugestaoSeq;
        var produtos = await _produtoService.ListarAsync(texto);
        if (seq != _sugestaoSeq)
            return;

        _sugestoes.Clear();
        _sugestoes.AddRange(produtos.Take(8).Select(p => new ProdutoSugestaoItem(
            p.Codigo,
            p.Descricao,
            p.PrecoVenda.ToString("C", _ptBr))));

        if (_sugestoes.Count == 0)
        {
            EsconderSugestoes();
            return;
        }

        LstSugestoes.ItemsSource = null;
        LstSugestoes.ItemsSource = _sugestoes;
        SugestoesBox.Visibility = Visibility.Visible;
    }

    private void EsconderSugestoes()
    {
        SugestoesBox.Visibility = Visibility.Collapsed;
        LstSugestoes.ItemsSource = null;
        _sugestoes.Clear();
    }

    private void AplicarSugestaoSelecionada(bool adicionarItemDepois)
    {
        if (LstSugestoes.SelectedItem is not ProdutoSugestaoItem s)
            return;

        _suspendSugestoes = true;
        TxtCodigo.Text = s.Codigo;
        _suspendSugestoes = false;
        EsconderSugestoes();
        TxtCodigo.Focus();
        TxtCodigo.SelectAll();
        if (adicionarItemDepois)
            _ = AdicionarItemAsync();
    }

    private void AplicarModoOperador(bool ativo)
    {
        var bg = ativo ? Color.FromRgb(15, 23, 42) : Color.FromRgb(244, 246, 250);
        var card = ativo ? Color.FromRgb(30, 41, 59) : Colors.White;
        var txt = ativo ? Colors.White : Color.FromRgb(15, 33, 61);
        var soft = ativo ? Color.FromRgb(148, 163, 184) : Color.FromRgb(93, 110, 135);

        Background = new SolidColorBrush(bg);
        RootGrid.Background = new SolidColorBrush(bg);
        foreach (var border in new[] { HeaderBorder, EntradaBorder, ItensBorder, TotalBorder })
        {
            border.Background = new SolidColorBrush(card);
            border.BorderBrush = new SolidColorBrush(ativo ? Color.FromRgb(51, 65, 85) : Color.FromRgb(220, 226, 236));
        }

        LblTituloPdv.Foreground = new SolidColorBrush(txt);
        LblNumeroVenda.Foreground = new SolidColorBrush(txt);
        LblTotal.Foreground = new SolidColorBrush(txt);
        LblTotalTitulo.Foreground = new SolidColorBrush(soft);

        DgItens.RowBackground = new SolidColorBrush(card);
        DgItens.AlternatingRowBackground = new SolidColorBrush(ativo ? Color.FromRgb(22, 32, 50) : Color.FromRgb(248, 250, 253));
        DgItens.Foreground = new SolidColorBrush(txt);
        DgItens.FontSize = ativo ? 16 : 13;
        DgItens.RowHeight = ativo ? 42 : double.NaN;
    }

    private async void TxtCodigo_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down && SugestoesBox.Visibility == Visibility.Visible && LstSugestoes.Items.Count > 0)
        {
            LstSugestoes.Focus();
            LstSugestoes.SelectedIndex = 0;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            if (SugestoesBox.Visibility == Visibility.Visible && LstSugestoes.SelectedItem != null)
            {
                AplicarSugestaoSelecionada(true);
                e.Handled = true;
                return;
            }
            await AdicionarItemAsync();
            e.Handled = true;
        }
    }

    private async void Finalizar_Click(object sender, RoutedEventArgs e)
    {
        await FinalizarAsync();
    }

    private async void Desconto_Click(object sender, RoutedEventArgs e)
    {
        await AplicarDescontoAsync();
    }

    private async void Remover_Click(object sender, RoutedEventArgs e)
    {
        await RemoverItemAsync();
    }

    private async void NovaVenda_Click(object sender, RoutedEventArgs e)
    {
        await IniciarNovaVendaAsync();
    }

    private async void CancelarVenda_Click(object sender, RoutedEventArgs e)
    {
        await CancelarVendaAsync();
    }

    private async void TxtCodigo_TextChanged(object sender, TextChangedEventArgs e)
    {
        await AtualizarSugestoesAsync();
    }

    private void LstSugestoes_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        AplicarSugestaoSelecionada(true);
    }

    private void LstSugestoes_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AplicarSugestaoSelecionada(true);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            EsconderSugestoes();
            TxtCodigo.Focus();
            e.Handled = true;
        }
    }

    private void TxtCodigo_GotFocus(object sender, RoutedEventArgs e)
    {
        _campoNumpad = TxtCodigo;
    }

    private void TxtQuantidade_GotFocus(object sender, RoutedEventArgs e)
    {
        _campoNumpad = TxtQuantidade;
    }

    private async void Numpad_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var token = btn.Content?.ToString() ?? "";

        if (token == "ADD")
        {
            await AdicionarItemAsync();
            return;
        }
        if (token == "COD")
        {
            _campoNumpad = TxtCodigo;
            TxtCodigo.Focus();
            return;
        }
        if (token == "QTD")
        {
            _campoNumpad = TxtQuantidade;
            TxtQuantidade.Focus();
            return;
        }
        if (token == "CLR")
        {
            _campoNumpad.Text = string.Empty;
            _campoNumpad.Focus();
            return;
        }
        if (token == "BK")
        {
            if (_campoNumpad.Text.Length > 0)
                _campoNumpad.Text = _campoNumpad.Text[..^1];
            _campoNumpad.Focus();
            return;
        }

        if (token == "," && _campoNumpad.Text.Contains(","))
            return;

        _campoNumpad.Text += token;
        _campoNumpad.CaretIndex = _campoNumpad.Text.Length;
        _campoNumpad.Focus();
    }

    private void ModoOperador_Changed(object sender, RoutedEventArgs e)
    {
        AplicarModoOperador(ChkModoOperador.IsChecked == true);
    }
}

public sealed record ItemVendaLinhaUi(
    int Id,
    string Codigo,
    string Descricao,
    string Quantidade,
    string PrecoUnitario,
    string Total);

public sealed record ProdutoSugestaoItem(
    string Codigo,
    string Descricao,
    string Preco)
{
    public override string ToString() => $"{Codigo} - {Descricao} ({Preco})";
}
