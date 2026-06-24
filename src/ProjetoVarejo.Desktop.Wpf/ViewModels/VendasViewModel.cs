using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using System.Collections.ObjectModel;

namespace ProjetoVarejo.Desktop.Wpf.ViewModels;

public partial class VendasViewModel : BaseViewModel
{
    private readonly IVendaService _vendaService;
    private readonly IProdutoService _produtoService;
    private readonly ICaixaService _caixaService;

    [ObservableProperty] private ObservableCollection<Venda> _vendas = [];
    [ObservableProperty] private ObservableCollection<Produto> _produtosPesquisa = [];
    [ObservableProperty] private ObservableCollection<ItemVenda> _itensVenda = [];
    [ObservableProperty] private ObservableCollection<FormaPagamentoTipo> _formasPagamento =
    [
        FormaPagamentoTipo.Dinheiro,
        FormaPagamentoTipo.Debito,
        FormaPagamentoTipo.Credito,
        FormaPagamentoTipo.Pix,
        FormaPagamentoTipo.Outros
    ];

    [ObservableProperty] private Venda? _vendaAtual;
    [ObservableProperty] private Venda? _vendaSelecionada;
    [ObservableProperty] private Produto? _produtoSelecionado;
    [ObservableProperty] private ItemVenda? _itemSelecionado;
    [ObservableProperty] private FormaPagamentoTipo _formaPagamentoSelecionada = FormaPagamentoTipo.Dinheiro;

    [ObservableProperty] private string _filtroProduto = string.Empty;
    [ObservableProperty] private decimal _quantidade = 1;
    [ObservableProperty] private decimal _desconto;
    [ObservableProperty] private decimal _valorPagamento;
    [ObservableProperty] private decimal _troco;

    [ObservableProperty] private string _numeroVendaAtual = "Sem venda aberta";
    [ObservableProperty] private string _statusVendaAtual = "-";
    [ObservableProperty] private string _caixaStatus = "Caixa nao verificado";
    [ObservableProperty] private string _total = "0 vendas";
    [ObservableProperty] private string _mensagem = string.Empty;
    [ObservableProperty] private string _erro = string.Empty;

    [ObservableProperty] private decimal _subtotal;
    [ObservableProperty] private decimal _totalVenda;

    public VendasViewModel(
        IVendaService vendaService,
        IProdutoService produtoService,
        ICaixaService caixaService)
    {
        _vendaService = vendaService;
        _produtoService = produtoService;
        _caixaService = caixaService;
    }

    partial void OnValorPagamentoChanged(decimal value)
    {
        Troco = value > TotalVenda ? value - TotalVenda : 0m;
    }

    [RelayCommand]
    private async Task CarregarAsync()
    {
        SetBusy(true, "Carregando PDV...");
        LimparMensagens();
        try
        {
            await CarregarVendasRecentesAsync();
            await AtualizarStatusCaixaAsync();
            if (VendaAtual != null)
            {
                await RecarregarVendaAtualAsync();
            }
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task NovaVendaAsync()
    {
        SetBusy(true, "Abrindo nova venda...");
        LimparMensagens();
        try
        {
            var caixa = await _caixaService.ObterCaixaAbertoAsync();
            if (caixa == null)
            {
                Erro = "Abra o caixa antes de iniciar uma venda.";
                await AtualizarStatusCaixaAsync();
                return;
            }

            var res = await _vendaService.NovaVendaAsync();
            if (!res.Sucesso || res.Valor == null)
            {
                Erro = res.Erro ?? "Nao foi possivel iniciar a venda.";
                return;
            }

            VendaAtual = res.Valor;
            await RecarregarVendaAtualAsync();
            Mensagem = $"Venda {VendaAtual.Numero} iniciada.";
            await CarregarVendasRecentesAsync();
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task PesquisarProdutosAsync()
    {
        SetBusy(true, "Pesquisando produtos...");
        LimparMensagens();
        try
        {
            var lista = await _produtoService.ListarParaVendaAsync(FiltroProduto);
            ProdutosPesquisa = new ObservableCollection<Produto>(lista);
            if (lista.Count == 1)
            {
                ProdutoSelecionado = lista[0];
            }
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task AdicionarItemAsync()
    {
        SetBusy(true, "Adicionando item...");
        LimparMensagens();
        try
        {
            if (VendaAtual == null)
            {
                Erro = "Inicie uma venda antes de adicionar itens.";
                return;
            }

            if (ProdutoSelecionado == null)
            {
                Erro = "Selecione um produto para adicionar.";
                return;
            }

            if (Quantidade <= 0)
            {
                Erro = "Quantidade deve ser maior que zero.";
                return;
            }

            var res = await _vendaService.AdicionarItemAsync(VendaAtual.Id, ProdutoSelecionado.Id, Quantidade);
            if (!res.Sucesso)
            {
                Erro = res.Erro ?? "Nao foi possivel adicionar o item.";
                return;
            }

            Quantidade = 1;
            await RecarregarVendaAtualAsync();
            await CarregarVendasRecentesAsync();
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task RemoverItemAsync()
    {
        SetBusy(true, "Removendo item...");
        LimparMensagens();
        try
        {
            if (VendaAtual == null || ItemSelecionado == null)
            {
                Erro = "Selecione um item da venda para remover.";
                return;
            }

            var res = await _vendaService.RemoverItemAsync(ItemSelecionado.Id);
            if (!res.Sucesso)
            {
                Erro = res.Erro ?? "Nao foi possivel remover o item.";
                return;
            }

            await RecarregarVendaAtualAsync();
            await CarregarVendasRecentesAsync();
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task AplicarDescontoAsync()
    {
        SetBusy(true, "Aplicando desconto...");
        LimparMensagens();
        try
        {
            if (VendaAtual == null)
            {
                Erro = "Nenhuma venda aberta para aplicar desconto.";
                return;
            }

            var res = await _vendaService.AplicarDescontoAsync(VendaAtual.Id, Desconto);
            if (!res.Sucesso)
            {
                Erro = res.Erro ?? "Nao foi possivel aplicar o desconto.";
                return;
            }

            await RecarregarVendaAtualAsync();
            await CarregarVendasRecentesAsync();
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task FinalizarAsync()
    {
        SetBusy(true, "Finalizando venda...");
        LimparMensagens();
        try
        {
            if (VendaAtual == null)
            {
                Erro = "Nenhuma venda aberta para finalizar.";
                return;
            }

            if (ItensVenda.Count == 0)
            {
                Erro = "Venda sem itens.";
                return;
            }

            var caixa = await _caixaService.ObterCaixaAbertoAsync();
            if (caixa == null)
            {
                Erro = "Abra o caixa antes de finalizar a venda.";
                await AtualizarStatusCaixaAsync();
                return;
            }

            var valor = ValorPagamento <= 0 ? TotalVenda : ValorPagamento;
            var pagamentos = new List<PagamentoVenda>
            {
                new()
                {
                    FormaPagamento = FormaPagamentoSelecionada,
                    Valor = valor,
                    Parcelas = 1
                }
            };

            var finalizar = await _vendaService.FinalizarAsync(VendaAtual.Id, pagamentos);
            if (!finalizar.Sucesso || finalizar.Valor == null)
            {
                Erro = finalizar.Erro ?? "Nao foi possivel finalizar a venda.";
                return;
            }

            var registrarCaixa = await _caixaService.RegistrarVendaAsync(caixa.Id, finalizar.Valor.Id, pagamentos);
            if (!registrarCaixa.Sucesso)
            {
                Erro = registrarCaixa.Erro ?? "Venda concluida, mas falhou registro no caixa.";
                return;
            }

            var troco = valor > finalizar.Valor.Total ? valor - finalizar.Valor.Total : 0m;
            Mensagem = $"Venda {finalizar.Valor.Numero} finalizada. Troco: {troco:C2}";
            LimparVendaAtual();
            await CarregarVendasRecentesAsync();
            await AtualizarStatusCaixaAsync();
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task CancelarVendaAsync()
    {
        SetBusy(true, "Cancelando venda...");
        LimparMensagens();
        try
        {
            if (VendaAtual == null)
            {
                Erro = "Nenhuma venda aberta para cancelar.";
                return;
            }

            var res = await _vendaService.CancelarAsync(VendaAtual.Id, "Cancelada pelo operador do PDV");
            if (!res.Sucesso)
            {
                Erro = res.Erro ?? "Nao foi possivel cancelar a venda.";
                return;
            }

            Mensagem = "Venda cancelada com sucesso.";
            LimparVendaAtual();
            await CarregarVendasRecentesAsync();
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    [RelayCommand]
    private async Task AbrirVendaAsync()
    {
        SetBusy(true, "Abrindo venda selecionada...");
        LimparMensagens();
        try
        {
            if (VendaSelecionada == null)
            {
                Erro = "Selecione uma venda na lista.";
                return;
            }

            var venda = await _vendaService.BuscarAsync(VendaSelecionada.Id);
            if (venda == null)
            {
                Erro = "Venda nao encontrada.";
                return;
            }

            VendaAtual = venda;
            AtualizarResumoVenda(venda);
        }
        catch (Exception ex)
        {
            Erro = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RecarregarVendaAtualAsync()
    {
        if (VendaAtual == null)
        {
            LimparVendaAtual();
            return;
        }

        var venda = await _vendaService.BuscarAsync(VendaAtual.Id);
        if (venda == null)
        {
            LimparVendaAtual();
            return;
        }

        VendaAtual = venda;
        AtualizarResumoVenda(venda);
    }

    private async Task CarregarVendasRecentesAsync()
    {
        var lista = await _vendaService.ListarAsync(DateTime.Today.AddDays(-7), DateTime.Today.AddDays(1));
        Vendas = new ObservableCollection<Venda>(lista);
        Total = $"{lista.Count} venda(s) nos ultimos 7 dias";
    }

    private async Task AtualizarStatusCaixaAsync()
    {
        var caixa = await _caixaService.ObterCaixaAbertoAsync();
        CaixaStatus = caixa == null
            ? "Caixa fechado"
            : $"Caixa aberto desde {caixa.AbertaEm:dd/MM/yyyy HH:mm}";
    }

    private void AtualizarResumoVenda(Venda venda)
    {
        ItensVenda = new ObservableCollection<ItemVenda>(venda.Itens.OrderBy(i => i.Id));
        NumeroVendaAtual = venda.Numero;
        StatusVendaAtual = venda.Status.ToString();
        Subtotal = venda.SubTotal;
        TotalVenda = venda.Total;
        Desconto = venda.Desconto;

        if (ValorPagamento <= 0)
        {
            ValorPagamento = venda.Total;
        }

        Troco = ValorPagamento > venda.Total ? ValorPagamento - venda.Total : 0m;
    }

    private void LimparVendaAtual()
    {
        VendaAtual = null;
        NumeroVendaAtual = "Sem venda aberta";
        StatusVendaAtual = "-";
        Subtotal = 0;
        TotalVenda = 0;
        Desconto = 0;
        ValorPagamento = 0;
        Troco = 0;
        ItensVenda = [];
        ItemSelecionado = null;
    }

    private void LimparMensagens()
    {
        Erro = string.Empty;
        Mensagem = string.Empty;
    }
}
