using ProjetoVarejo.Application.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace ProjetoVarejo.Desktop.Erp;

public partial class MainWindow : Window
{
    private readonly FornecedorService _fornecedorService;
    private readonly ProdutoService _produtoService;
    private readonly PedidoCompraService _pedidoCompraService;

    private readonly ObservableCollection<FornecedorRow> _fornecedores = new();
    private readonly ObservableCollection<ProdutoRow> _produtos = new();
    private readonly ObservableCollection<PedidoItemRow> _itensPedido = new();
    private readonly ObservableCollection<PedidoCompraRow> _pedidos = new();
    private string _moduloAtivo = "Compras e Suprimentos";
    private int? _pedidoEmEdicaoId;

    public MainWindow(FornecedorService fornecedorService, ProdutoService produtoService, PedidoCompraService pedidoCompraService)
    {
        _fornecedorService = fornecedorService;
        _produtoService = produtoService;
        _pedidoCompraService = pedidoCompraService;
        InitializeComponent();

        DgFornecedores.ItemsSource = _fornecedores;
        CmbProdutoItem.ItemsSource = _produtos;
        LvItensPedido.ItemsSource = _itensPedido;
        DgPedidos.ItemsSource = _pedidos;
        CmbStatusPedido.SelectedIndex = 0;
        CmbFiltroStatus.SelectedIndex = 0;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AtualizarTitulosModulo("Compras e Suprimentos");
        await CarregarFornecedoresAsync();
        await CarregarProdutosAsync();
        await CarregarPedidosAsync();
        AtualizarModoPedidoUI();
    }

    private async Task CarregarProdutosAsync(string? filtro = null)
    {
        try
        {
            var lista = await _produtoService.ListarParaVendaAsync(filtro);
            _produtos.Clear();

            foreach (var p in lista)
            {
                _produtos.Add(new ProdutoRow(p.Id, p.Codigo, p.Descricao, p.PrecoCusto));
            }
        }
        catch (Exception ex)
        {
            TxtMensagemModulo.Text = $"Falha ao carregar produtos: {ex.Message}";
        }
    }

    private async Task CarregarFornecedoresAsync(string? filtro = null)
    {
        try
        {
            BtnAtualizarFornecedores.IsEnabled = false;
            var lista = await _fornecedorService.ListarAsync(filtro);
            _fornecedores.Clear();

            foreach (var f in lista)
            {
                _fornecedores.Add(new FornecedorRow(
                    f.Id,
                    f.RazaoSocial,
                    f.NomeFantasia ?? string.Empty,
                    f.Cnpj,
                    $"{f.Cidade ?? "-"} / {f.Uf ?? "-"}",
                    f.Telefone ?? "-"));
            }

            TxtMensagemModulo.Text = $"Compras e Suprimentos conectado com banco: {_fornecedores.Count} fornecedor(es) carregado(s).";
        }
        catch (Exception ex)
        {
            TxtMensagemModulo.Text = $"Falha ao carregar fornecedores: {ex.Message}";
        }
        finally
        {
            BtnAtualizarFornecedores.IsEnabled = true;
        }
    }

    private async Task CarregarPedidosAsync()
    {
        try
        {
            var status = ObterStatusFiltro();
            var de = DpFiltroDe.SelectedDate;
            var ate = DpFiltroAte.SelectedDate;

            var pedidos = await _pedidoCompraService.ListarAsync(status, de, ate);
            _pedidos.Clear();
            foreach (var p in pedidos)
            {
                _pedidos.Add(new PedidoCompraRow(
                    p.Id,
                    p.Numero,
                    p.FornecedorId,
                    p.Fornecedor?.NomeFantasia ?? p.Fornecedor?.RazaoSocial ?? "-",
                    p.DataEmissao.ToString("dd/MM/yyyy HH:mm"),
                    p.Total.ToString("C2", new CultureInfo("pt-BR")),
                    p.Status));
            }
        }
        catch (Exception ex)
        {
            TxtMensagemModulo.Text = $"Falha ao carregar pedidos: {ex.Message}";
        }
    }

    private async void BtnAtualizarFornecedores_Click(object sender, RoutedEventArgs e)
    {
        await CarregarFornecedoresAsync(TxtFiltroFornecedor.Text);
    }

    private async void BtnAtualizarPedidos_Click(object sender, RoutedEventArgs e)
    {
        await CarregarPedidosAsync();
    }

    private async void BtnFiltrarPedidos_Click(object sender, RoutedEventArgs e)
    {
        await CarregarPedidosAsync();
    }

    private async void BtnLimparFiltroPedidos_Click(object sender, RoutedEventArgs e)
    {
        CmbFiltroStatus.SelectedIndex = 0;
        DpFiltroDe.SelectedDate = null;
        DpFiltroAte.SelectedDate = null;
        await CarregarPedidosAsync();
    }

    private async void TxtFiltroFornecedor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_moduloAtivo != "Compras e Suprimentos")
        {
            return;
        }

        await CarregarFornecedoresAsync(TxtFiltroFornecedor.Text);
    }

    private void BtnAdicionarItem_Click(object sender, RoutedEventArgs e)
    {
        if (CmbProdutoItem.SelectedItem is not ProdutoRow produtoSelecionado)
        {
            TxtMensagemModulo.Text = "Selecione um produto para vincular ao item.";
            return;
        }

        var descricao = TxtItemDescricao.Text.Trim();
        if (string.IsNullOrWhiteSpace(descricao))
        {
            TxtMensagemModulo.Text = "Informe a descricao do item antes de adicionar.";
            return;
        }

        if (!TryParseDecimal(TxtQtd.Text, out var qtd) || qtd <= 0)
        {
            TxtMensagemModulo.Text = "Quantidade invalida. Use valor maior que zero.";
            return;
        }

        if (!TryParseDecimal(TxtValorUnitario.Text, out var valorUnitario) || valorUnitario <= 0)
        {
            TxtMensagemModulo.Text = "Valor unitario invalido. Use valor maior que zero.";
            return;
        }

        var item = new PedidoItemRow(null, produtoSelecionado.Id, produtoSelecionado.Codigo, descricao, qtd, 0, valorUnitario);
        _itensPedido.Add(item);
        AtualizarTotalPedido();

        CmbProdutoItem.SelectedItem = null;
        TxtItemDescricao.Text = string.Empty;
        TxtQtd.Text = string.Empty;
        TxtValorUnitario.Text = string.Empty;
        TxtMensagemModulo.Text = $"Item \"{descricao}\" adicionado ao pedido.";
    }

    private void BtnRemoverItem_Click(object sender, RoutedEventArgs e)
    {
        if (LvItensPedido.SelectedItem is not PedidoItemRow itemSelecionado)
        {
            TxtMensagemModulo.Text = "Selecione um item da lista para remover.";
            return;
        }

        _itensPedido.Remove(itemSelecionado);
        AtualizarTotalPedido();
        TxtMensagemModulo.Text = $"Item \"{itemSelecionado.Descricao}\" removido.";
    }

    private void BtnLimparItens_Click(object sender, RoutedEventArgs e)
    {
        if (_itensPedido.Count == 0)
        {
            TxtMensagemModulo.Text = "Nao ha itens para limpar.";
            return;
        }

        _itensPedido.Clear();
        AtualizarTotalPedido();
        TxtMensagemModulo.Text = "Lista de itens limpa.";
    }

    private async void BtnSalvarPedido_Click(object sender, RoutedEventArgs e)
    {
        if (DgFornecedores.SelectedItem is not FornecedorRow fornecedor)
        {
            TxtMensagemModulo.Text = "Selecione um fornecedor na lista para salvar o pedido.";
            return;
        }

        if (_itensPedido.Count == 0)
        {
            TxtMensagemModulo.Text = "Adicione ao menos um item para salvar pedido.";
            return;
        }

        var itens = _itensPedido.Select(i =>
            new PedidoCompraItemInput(i.Descricao, i.Quantidade, i.ValorUnitario, i.ProdutoId));
        var observacao = string.IsNullOrWhiteSpace(TxtObservacaoPedido.Text)
            ? null
            : TxtObservacaoPedido.Text.Trim();

        if (_pedidoEmEdicaoId.HasValue)
        {
            var resultUpdate = await _pedidoCompraService.AtualizarAsync(_pedidoEmEdicaoId.Value, fornecedor.Id, observacao, itens);
            if (!resultUpdate.Sucesso)
            {
                TxtMensagemModulo.Text = resultUpdate.Erro ?? "Falha ao atualizar pedido.";
                return;
            }

            TxtMensagemModulo.Text = $"Pedido {resultUpdate.Valor?.Numero} atualizado com sucesso.";
        }
        else
        {
            var resultCreate = await _pedidoCompraService.CriarAsync(fornecedor.Id, observacao, itens);
            if (!resultCreate.Sucesso)
            {
                TxtMensagemModulo.Text = resultCreate.Erro ?? "Falha ao salvar pedido.";
                return;
            }

            TxtMensagemModulo.Text = $"Pedido {resultCreate.Valor?.Numero} salvo com sucesso.";
        }

        await CarregarPedidosAsync();
        LimparFormularioPedido();
    }

    private async void BtnEditarPedido_Click(object sender, RoutedEventArgs e)
    {
        if (DgPedidos.SelectedItem is not PedidoCompraRow row)
        {
            TxtMensagemModulo.Text = "Selecione um pedido para editar.";
            return;
        }

        await CarregarPedidoParaEdicaoAsync(row.Id);
    }

    private async Task CarregarPedidoParaEdicaoAsync(int pedidoId)
    {
        var pedido = await _pedidoCompraService.ObterPorIdAsync(pedidoId);
        if (pedido == null)
        {
            TxtMensagemModulo.Text = "Pedido nao encontrado para edicao.";
            return;
        }

        _pedidoEmEdicaoId = pedido.Id;
        _itensPedido.Clear();
        foreach (var item in pedido.Itens.OrderBy(i => i.Id))
        {
            _itensPedido.Add(new PedidoItemRow(
                item.Id,
                item.ProdutoId,
                item.Produto?.Codigo ?? "-",
                item.Descricao,
                item.Quantidade,
                item.QuantidadeRecebida,
                item.ValorUnitario));
        }
        TxtObservacaoPedido.Text = pedido.Observacao ?? string.Empty;

        AtualizarTotalPedido();
        await SelecionarFornecedorNoGridAsync(pedido.FornecedorId);
        AtualizarModoPedidoUI();
        TxtMensagemModulo.Text = $"Pedido {pedido.Numero} carregado para edicao.";
    }

    private async void BtnReceberItem_Click(object sender, RoutedEventArgs e)
    {
        if (DgPedidos.SelectedItem is not PedidoCompraRow pedido)
        {
            TxtMensagemModulo.Text = "Selecione um pedido para conferir recebimento.";
            return;
        }

        if (LvItensPedido.SelectedItem is not PedidoItemRow item || !item.ItemId.HasValue)
        {
            TxtMensagemModulo.Text = "Selecione um item ja salvo no pedido para receber.";
            return;
        }

        if (!TryParseDecimal(TxtQtdReceber.Text, out var qtdReceber) || qtdReceber <= 0)
        {
            TxtMensagemModulo.Text = "Informe uma quantidade recebida valida.";
            return;
        }

        decimal? valorRecebido = null;
        if (!string.IsNullOrWhiteSpace(TxtVlrUnitRecebido.Text))
        {
            if (!TryParseDecimal(TxtVlrUnitRecebido.Text, out var valorTmp) || valorTmp <= 0)
            {
                TxtMensagemModulo.Text = "Valor unitario recebido invalido.";
                return;
            }
            valorRecebido = valorTmp;
        }

        var result = await _pedidoCompraService.ReceberItemAsync(
            pedido.Id,
            item.ItemId.Value,
            qtdReceber,
            valorRecebido,
            "Conferencia manual de recebimento");

        if (!result.Sucesso || result.Valor == null)
        {
            TxtMensagemModulo.Text = result.Erro ?? "Falha ao receber item.";
            return;
        }

        await CarregarPedidosAsync();
        await CarregarPedidoParaEdicaoAsync(pedido.Id);
        TxtQtdReceber.Text = string.Empty;
        TxtVlrUnitRecebido.Text = string.Empty;

        var resumo = result.Valor;
        var divergencia = resumo.HouveDivergenciaValor ? " (divergencia de valor registrada)" : string.Empty;
        TxtMensagemModulo.Text =
            $"Item recebido: {resumo.QuantidadeRecebidaAgora:N3}. " +
            $"Pendente: {resumo.QuantidadePendente:N3}.{divergencia}";
    }

    private async void BtnHistoricoRecebimento_Click(object sender, RoutedEventArgs e)
    {
        if (DgPedidos.SelectedItem is not PedidoCompraRow pedido)
        {
            TxtMensagemModulo.Text = "Selecione um pedido para ver o historico.";
            return;
        }

        var itemSelecionadoId = (LvItensPedido.SelectedItem as PedidoItemRow)?.ItemId;
        var result = await _pedidoCompraService.ListarHistoricoRecebimentoAsync(pedido.Id, itemSelecionadoId);
        if (!result.Sucesso || result.Valor == null)
        {
            TxtMensagemModulo.Text = result.Erro ?? "Falha ao carregar historico.";
            return;
        }

        var linhas = result.Valor.Select(h => new RecebimentoHistoricoLinhaUi(
            h.DataHora,
            h.Usuario,
            $"{h.ProdutoCodigo} - {h.ProdutoDescricao}",
            h.Quantidade,
            h.ValorPlanejado,
            h.ValorRecebido,
            h.Divergencia,
            h.Observacao)).ToList();

        var contextoItem = itemSelecionadoId.HasValue ? " (item selecionado)" : string.Empty;
        var titulo = $"Historico de recebimento - Pedido {pedido.Numero}{contextoItem}";
        var janela = new RecebimentoHistoricoWindow(titulo, linhas)
        {
            Owner = this
        };
        janela.ShowDialog();
    }

    private void BtnNovoPedido_Click(object sender, RoutedEventArgs e)
    {
        LimparFormularioPedido();
        TxtMensagemModulo.Text = "Formulario preparado para novo pedido.";
    }

    private async Task SelecionarFornecedorNoGridAsync(int fornecedorId)
    {
        var row = _fornecedores.FirstOrDefault(f => f.Id == fornecedorId);
        if (row == null)
        {
            await CarregarFornecedoresAsync();
            row = _fornecedores.FirstOrDefault(f => f.Id == fornecedorId);
        }

        if (row != null)
        {
            DgFornecedores.SelectedItem = row;
            DgFornecedores.ScrollIntoView(row);
        }
    }

    private async void BtnAtualizarStatus_Click(object sender, RoutedEventArgs e)
    {
        if (DgPedidos.SelectedItem is not PedidoCompraRow pedido)
        {
            TxtMensagemModulo.Text = "Selecione um pedido para atualizar status.";
            return;
        }

        if (CmbStatusPedido.SelectedItem is not ComboBoxItem item || item.Content is not string status)
        {
            TxtMensagemModulo.Text = "Selecione um status valido.";
            return;
        }

        var result = await _pedidoCompraService.AtualizarStatusAsync(pedido.Id, status);
        if (!result.Sucesso)
        {
            TxtMensagemModulo.Text = result.Erro ?? "Falha ao atualizar status.";
            return;
        }

        await CarregarPedidosAsync();
        TxtMensagemModulo.Text = $"Pedido {pedido.Numero} atualizado para {status}.";
    }

    private async void BtnReceberPedido_Click(object sender, RoutedEventArgs e)
    {
        if (DgPedidos.SelectedItem is not PedidoCompraRow pedido)
        {
            TxtMensagemModulo.Text = "Selecione um pedido para receber.";
            return;
        }

        var confirm = MessageBox.Show(
            $"Confirmar recebimento do pedido {pedido.Numero} e lancar entrada no estoque?",
            "Receber pedido",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var result = await _pedidoCompraService.ReceberComEntradaEstoqueAsync(pedido.Id);
        if (!result.Sucesso || result.Valor == null)
        {
            TxtMensagemModulo.Text = result.Erro ?? "Falha ao receber pedido.";
            return;
        }

        if (_pedidoEmEdicaoId == pedido.Id)
        {
            LimparFormularioPedido();
        }

        await CarregarPedidosAsync();
        var resumo = result.Valor;
        TxtMensagemModulo.Text =
            $"Pedido {resumo.NumeroPedido} recebido: {resumo.QuantidadeItens} item(ns), " +
            $"movimentacao total {resumo.QuantidadeTotalMovimentada:N3}.";
    }

    private async void BtnExcluirPedido_Click(object sender, RoutedEventArgs e)
    {
        if (DgPedidos.SelectedItem is not PedidoCompraRow pedido)
        {
            TxtMensagemModulo.Text = "Selecione um pedido para excluir.";
            return;
        }

        var confirm = MessageBox.Show(
            $"Deseja excluir o pedido {pedido.Numero}?",
            "Confirmar exclusao",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var result = await _pedidoCompraService.ExcluirAsync(pedido.Id);
        if (!result.Sucesso)
        {
            TxtMensagemModulo.Text = result.Erro ?? "Falha ao excluir pedido.";
            return;
        }

        if (_pedidoEmEdicaoId == pedido.Id)
        {
            LimparFormularioPedido();
        }

        await CarregarPedidosAsync();
        TxtMensagemModulo.Text = $"Pedido {pedido.Numero} excluido.";
    }

    private void SelecionarModulo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string modulo)
        {
            return;
        }

        _moduloAtivo = modulo;
        AtualizarTitulosModulo(modulo);
    }

    private void AtualizarTitulosModulo(string modulo)
    {
        TxtStatusTopo.Text = $"Modulo ativo: {modulo}";

        if (modulo == "Compras e Suprimentos")
        {
            TxtSubtituloTopo.Text = "Gestao corporativa com foco em processos internos";
            TxtMensagemModulo.Text = "Compras e Suprimentos conectado com banco: carregando fornecedores...";
            PainelCompras.Visibility = Visibility.Visible;
            PainelPlaceholder.Visibility = Visibility.Collapsed;
            _ = CarregarFornecedoresAsync(TxtFiltroFornecedor.Text);
            _ = CarregarPedidosAsync();
            return;
        }

        PainelCompras.Visibility = Visibility.Collapsed;
        PainelPlaceholder.Visibility = Visibility.Visible;

        TxtTituloPlaceholder.Text = modulo;
        TxtDescricaoPlaceholder.Text = modulo switch
        {
            "Producao e PCP" => "Proxima fase: ordens de producao, apontamento de eficiencia e controle de perdas.",
            "Financeiro Corporativo" => "Proxima fase: fluxo consolidado, DRE por centro de custo e conciliacao.",
            "Fiscal e Contabil" => "Proxima fase: apuracoes fiscais, compliance e fechamento contabil.",
            "RH e Folha" => "Proxima fase: ponto, beneficios, folha e indicadores de equipe.",
            "BI e Indicadores" => "Proxima fase: dashboards executivos com metas e alertas.",
            _ => "Modulo em implantacao nas proximas fases."
        };
        TxtMensagemModulo.Text = $"Modulo {modulo} preparado para implementacao.";
    }

    private void LimparFormularioPedido()
    {
        _pedidoEmEdicaoId = null;
        _itensPedido.Clear();
        CmbProdutoItem.SelectedItem = null;
        TxtObservacaoPedido.Text = string.Empty;
        TxtItemDescricao.Text = string.Empty;
        TxtQtd.Text = string.Empty;
        TxtValorUnitario.Text = string.Empty;
        TxtQtdReceber.Text = string.Empty;
        TxtVlrUnitRecebido.Text = string.Empty;
        DgFornecedores.UnselectAll();
        AtualizarTotalPedido();
        AtualizarModoPedidoUI();
    }

    private void AtualizarModoPedidoUI()
    {
        if (_pedidoEmEdicaoId.HasValue)
        {
            TxtModoPedido.Text = $"Modo: editando pedido #{_pedidoEmEdicaoId.Value}";
            BtnSalvarPedido.Content = "Atualizar pedido";
        }
        else
        {
            TxtModoPedido.Text = "Modo: novo pedido";
            BtnSalvarPedido.Content = "Salvar pedido";
        }
    }

    private void AtualizarTotalPedido()
    {
        var total = _itensPedido.Sum(i => i.Subtotal);
        TxtTotalPedido.Text = total.ToString("C2", new CultureInfo("pt-BR"));
    }

    private string? ObterStatusFiltro()
    {
        if (CmbFiltroStatus.SelectedItem is not ComboBoxItem item || item.Content is not string status)
        {
            return null;
        }

        return status;
    }

    private void CmbProdutoItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CmbProdutoItem.SelectedItem is not ProdutoRow produto)
        {
            return;
        }

        TxtItemDescricao.Text = produto.Descricao;
        if (string.IsNullOrWhiteSpace(TxtValorUnitario.Text) && produto.PrecoCusto > 0)
        {
            TxtValorUnitario.Text = produto.PrecoCusto.ToString("N2", new CultureInfo("pt-BR"));
        }
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        var okPtBr = decimal.TryParse(text, NumberStyles.Number, new CultureInfo("pt-BR"), out value);
        if (okPtBr)
        {
            return true;
        }

        var normalized = text.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private sealed record FornecedorRow(
        int Id,
        string RazaoSocial,
        string NomeFantasia,
        string Cnpj,
        string CidadeUf,
        string Telefone);

    private sealed record ProdutoRow(int Id, string Codigo, string Descricao, decimal PrecoCusto)
    {
        public string Exibicao => $"{Codigo} - {Descricao}";
    }

    private sealed record PedidoItemRow(int? ItemId, int? ProdutoId, string ProdutoCodigo, string Descricao, decimal Quantidade, decimal QuantidadeRecebida, decimal ValorUnitario)
    {
        public string ProdutoRef => ProdutoCodigo;
        public decimal QuantidadePendente => Math.Max(0, Quantidade - QuantidadeRecebida);
        public string QuantidadeRecebidaFormatada => QuantidadeRecebida.ToString("N3", new CultureInfo("pt-BR"));
        public string QuantidadePendenteFormatada => QuantidadePendente.ToString("N3", new CultureInfo("pt-BR"));
        public decimal Subtotal => Quantidade * ValorUnitario;
        public string ValorUnitarioFormatado => ValorUnitario.ToString("C2", new CultureInfo("pt-BR"));
        public string SubtotalFormatado => Subtotal.ToString("C2", new CultureInfo("pt-BR"));
    }

    private sealed record PedidoCompraRow(
        int Id,
        string Numero,
        int FornecedorId,
        string Fornecedor,
        string DataEmissao,
        string Total,
        string Status);
}
