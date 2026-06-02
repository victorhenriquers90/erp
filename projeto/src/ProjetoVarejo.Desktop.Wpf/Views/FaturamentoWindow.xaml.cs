using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Desktop.Wpf.Views;

public partial class FaturamentoWindow : UserControl
{
    private readonly ClienteService _clienteService;
    private readonly ProdutoService _produtoService;
    private readonly VendaService _vendaService;
    private readonly NfeService _nfeService;
    private readonly CultureInfo _ptBr = new("pt-BR");

    private readonly ObservableCollection<ItemUi> _itens = [];

    public FaturamentoWindow(ClienteService clienteService, ProdutoService produtoService,
        VendaService vendaService, NfeService nfeService)
    {
        _clienteService = clienteService;
        _produtoService = produtoService;
        _vendaService = vendaService;
        _nfeService = nfeService;
        InitializeComponent();
        DgItens.ItemsSource = _itens;
        Loaded += async (_, _) => await CarregarAsync();
    }

    private async Task CarregarAsync()
    {
        CmbCliente.ItemsSource = await _clienteService.ListarAsync();
        CmbProduto.ItemsSource = await _produtoService.ListarAsync();
    }

    private void AddItem_Click(object sender, RoutedEventArgs e)
    {
        if (CmbProduto.SelectedItem is not Produto produto)
        {
            MessageBox.Show("Selecione um produto.", "Faturamento", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!decimal.TryParse(TxtQtd.Text.Replace(".", ","), NumberStyles.Any, _ptBr, out var qtd) || qtd <= 0)
        {
            MessageBox.Show("Informe uma quantidade válida.", "Faturamento", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _itens.Add(new ItemUi
        {
            ProdutoId = produto.Id,
            Descricao = produto.Descricao,
            Quantidade = qtd,
            Preco = produto.PrecoVenda,
            PtBr = _ptBr
        });
        AtualizarTotal();
        TxtQtd.Text = "1";
    }

    private void AtualizarTotal()
    {
        var total = _itens.Sum(i => i.Total);
        LblTotal.Text = total.ToString("C2", _ptBr);
    }

    private async void Emitir_Click(object sender, RoutedEventArgs e)
    {
        ResultadoBox.Visibility = Visibility.Collapsed;

        if (CmbCliente.SelectedItem is not Cliente cliente)
        {
            MessageBox.Show("Selecione o cliente destinatário.", "Faturamento", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (string.IsNullOrWhiteSpace(cliente.CpfCnpj))
        {
            MessageBox.Show("O cliente selecionado não tem CPF/CNPJ — obrigatório para NF-e.", "Faturamento", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_itens.Count == 0)
        {
            MessageBox.Show("Adicione ao menos um item.", "Faturamento", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        BtnEmitir.IsEnabled = false;
        BtnEmitir.Content = "Emitindo...";
        try
        {
            // 1) cria a venda/faturamento
            var nova = await _vendaService.NovaVendaAsync();
            if (!nova.Sucesso) { MostrarResultado(false, nova.Erro!); return; }
            var venda = nova.Valor!;

            // 2) adiciona itens
            foreach (var item in _itens)
            {
                var add = await _vendaService.AdicionarItemAsync(venda.Id, item.ProdutoId, item.Quantidade);
                if (!add.Sucesso) { MostrarResultado(false, add.Erro!); return; }
            }

            // 3) finaliza (pagamento = total; faturamento B2B)
            await _vendaService.RecalcularTotaisAsync(venda.Id);
            var atual = await _vendaService.BuscarAsync(venda.Id);
            var total = atual?.Total ?? _itens.Sum(i => i.Total);
            var pagamentos = new List<PagamentoVenda>
            {
                new() { FormaPagamento = FormaPagamentoTipo.Outros, Valor = total }
            };
            var fin = await _vendaService.FinalizarAsync(venda.Id, pagamentos, cliente.Id);
            if (!fin.Sucesso) { MostrarResultado(false, fin.Erro!); return; }

            // 4) emite a NF-e
            var emis = await _nfeService.EmitirAsync(venda.Id);
            if (emis.Sucesso)
            {
                var nota = emis.Valor!;
                MostrarResultado(true,
                    $"NF-e autorizada!  Número {nota.Numero}/{nota.Serie}\nChave: {nota.ChaveAcesso}\nProtocolo: {nota.Protocolo}");
                _itens.Clear();
                AtualizarTotal();
            }
            else
            {
                MostrarResultado(false, emis.Erro!);
            }
        }
        catch (Exception ex)
        {
            MostrarResultado(false, "Erro ao emitir: " + ex.Message);
        }
        finally
        {
            BtnEmitir.IsEnabled = true;
            BtnEmitir.Content = "Emitir NF-e";
        }
    }

    private void MostrarResultado(bool sucesso, string mensagem)
    {
        LblResultado.Text = mensagem;
        LblResultado.Foreground = new SolidColorBrush(sucesso ? Color.FromRgb(6, 95, 70) : Color.FromRgb(180, 35, 24));
        ResultadoBox.Background = new SolidColorBrush(sucesso ? Color.FromRgb(236, 253, 245) : Color.FromRgb(254, 242, 242));
        ResultadoBox.BorderBrush = new SolidColorBrush(sucesso ? Color.FromRgb(167, 243, 208) : Color.FromRgb(252, 165, 165));
        ResultadoBox.Visibility = Visibility.Visible;
    }

    public sealed class ItemUi
    {
        public int ProdutoId { get; init; }
        public string Descricao { get; init; } = "";
        public decimal Quantidade { get; init; }
        public decimal Preco { get; init; }
        public decimal Total => Quantidade * Preco;
        public CultureInfo PtBr { get; init; } = CultureInfo.InvariantCulture;
        public string PrecoTexto => Preco.ToString("C2", PtBr);
        public string TotalTexto => Total.ToString("C2", PtBr);
    }
}
