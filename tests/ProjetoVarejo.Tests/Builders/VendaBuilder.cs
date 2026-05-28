using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Tests.Builders;

/// <summary>
/// Fluent builder for creating test Venda entities.
/// Provides default values and convenience methods for common scenarios.
/// </summary>
public class VendaBuilder
{
    private int _id = 1;
    private string _numero = "0001";
    private DateTime _dataVenda = DateTime.Now;
    private int? _clienteId = null;
    private Cliente? _cliente = null;
    private int _usuarioId = 1;
    private Usuario? _usuario = null;
    private decimal _subTotal = 100m;
    private decimal _desconto = 0m;
    private decimal _acrescimo = 0m;
    private decimal _total = 100m;
    private decimal _valorPago = 100m;
    private decimal _troco = 0m;
    private StatusVenda _status = StatusVenda.EmAberto;
    private string? _observacao = null;
    private DateTime? _finalizadaEm = null;
    private DateTime? _canceladaEm = null;
    private int? _notaFiscalId = null;
    private List<ItemVenda> _itens = new();

    public VendaBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public VendaBuilder WithNumero(string numero)
    {
        _numero = numero;
        return this;
    }

    public VendaBuilder WithDataVenda(DateTime data)
    {
        _dataVenda = data;
        return this;
    }

    public VendaBuilder WithClienteId(int? clienteId)
    {
        _clienteId = clienteId;
        return this;
    }

    public VendaBuilder WithCliente(Cliente? cliente)
    {
        _cliente = cliente;
        return this;
    }

    public VendaBuilder WithUsuarioId(int usuarioId)
    {
        _usuarioId = usuarioId;
        return this;
    }

    public VendaBuilder WithUsuario(Usuario? usuario)
    {
        _usuario = usuario;
        return this;
    }

    public VendaBuilder WithSubTotal(decimal subTotal)
    {
        _subTotal = subTotal;
        _total = subTotal - _desconto + _acrescimo;
        _valorPago = _total;
        return this;
    }

    public VendaBuilder WithDesconto(decimal desconto)
    {
        _desconto = desconto;
        _total = _subTotal - desconto + _acrescimo;
        _valorPago = _total;
        return this;
    }

    public VendaBuilder WithAcrescimo(decimal acrescimo)
    {
        _acrescimo = acrescimo;
        _total = _subTotal - _desconto + acrescimo;
        _valorPago = _total;
        return this;
    }

    public VendaBuilder WithTotal(decimal total)
    {
        _total = total;
        _valorPago = total;
        return this;
    }

    public VendaBuilder WithValorPago(decimal valorPago)
    {
        _valorPago = valorPago;
        _troco = valorPago - _total;
        return this;
    }

    public VendaBuilder WithStatus(StatusVenda status)
    {
        _status = status;
        if (status == StatusVenda.Finalizada && _finalizadaEm == null)
            _finalizadaEm = DateTime.Now;
        if (status == StatusVenda.Cancelada && _canceladaEm == null)
            _canceladaEm = DateTime.Now;
        return this;
    }

    public VendaBuilder WithObservacao(string? observacao)
    {
        _observacao = observacao;
        return this;
    }

    public VendaBuilder WithFinalizadaEm(DateTime? data)
    {
        _finalizadaEm = data;
        return this;
    }

    public VendaBuilder WithCanceladaEm(DateTime? data)
    {
        _canceladaEm = data;
        return this;
    }

    public VendaBuilder WithNotaFiscalId(int? notaFiscalId)
    {
        _notaFiscalId = notaFiscalId;
        return this;
    }

    /// <summary>
    /// Add item to venda. Auto-calculates totals.
    /// </summary>
    public VendaBuilder AddItem(int produtoId, decimal quantidade, decimal precoUnitario)
    {
        var itemTotal = quantidade * precoUnitario;
        var item = new ItemVenda
        {
            Id = _itens.Count + 1,
            VendaId = _id,
            ProdutoId = produtoId,
            Quantidade = quantidade,
            PrecoUnitario = precoUnitario,
            Total = itemTotal
        };
        _itens.Add(item);

        // Recalculate totals
        _subTotal = _itens.Sum(i => i.Total);
        _total = _subTotal - _desconto + _acrescimo;
        _valorPago = _total;

        return this;
    }

    public Venda Build()
    {
        var venda = new Venda
        {
            Id = _id,
            Numero = _numero,
            DataVenda = _dataVenda,
            ClienteId = _clienteId,
            Cliente = _cliente,
            UsuarioId = _usuarioId,
            Usuario = _usuario ?? new UsuarioBuilder().WithId(_usuarioId).Build(),
            SubTotal = _subTotal,
            Desconto = _desconto,
            Acrescimo = _acrescimo,
            Total = _total,
            ValorPago = _valorPago,
            Troco = _troco,
            Status = _status,
            Observacao = _observacao,
            FinalizadaEm = _finalizadaEm,
            CanceladaEm = _canceladaEm,
            NotaFiscalId = _notaFiscalId,
            Itens = _itens
        };
        return venda;
    }

    /// <summary>
    /// Create a simple venda with default values.
    /// </summary>
    public static Venda CreateSimple(int id = 1, int usuarioId = 1)
    {
        return new VendaBuilder()
            .WithId(id)
            .WithNumero($"{id:D4}")
            .WithUsuarioId(usuarioId)
            .Build();
    }

    /// <summary>
    /// Create a finalized venda with items.
    /// </summary>
    public static Venda CreateFinalized(int id = 1, int usuarioId = 1)
    {
        var venda = new VendaBuilder()
            .WithId(id)
            .WithNumero($"{id:D4}")
            .WithUsuarioId(usuarioId)
            .WithStatus(StatusVenda.Finalizada)
            .WithFinalizadaEm(DateTime.Now.AddHours(-1));

        // Add some default items
        venda.AddItem(1, 2, 50m);  // 2x product 1 at 50 = 100
        venda.AddItem(2, 1, 50m);  // 1x product 2 at 50 = 50

        return venda.Build();
    }

    /// <summary>
    /// Create a cancelled venda.
    /// </summary>
    public static Venda CreateCancelled(int id = 1, int usuarioId = 1)
    {
        return new VendaBuilder()
            .WithId(id)
            .WithNumero($"{id:D4}")
            .WithUsuarioId(usuarioId)
            .WithStatus(StatusVenda.Cancelada)
            .WithCanceladaEm(DateTime.Now.AddHours(-2))
            .Build();
    }

    /// <summary>
    /// Create a venda with multiple items.
    /// </summary>
    public static Venda CreateWithItems(int id = 1, int itemCount = 3)
    {
        var venda = new VendaBuilder()
            .WithId(id)
            .WithNumero($"{id:D4}");

        for (int i = 1; i <= itemCount; i++)
        {
            venda.AddItem(i, i, 10m * i);
        }

        return venda.Build();
    }
}
