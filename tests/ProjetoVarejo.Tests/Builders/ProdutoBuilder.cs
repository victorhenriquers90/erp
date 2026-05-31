using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Tests.Builders;

/// <summary>
/// Fluent builder for creating test Produto entities.
/// Provides default values and batch creation methods.
/// </summary>
public class ProdutoBuilder
{
    private int _id = 1;
    private string _codigo = "PROD001";
    private string _codigoBarras = "1234567890123";
    private string _descricao = "Test Product";
    private int? _categoriaId = null;
    private UnidadeMedida _unidade = UnidadeMedida.UN;
    private decimal _precoCusto = 10m;
    private decimal _precoVenda = 20m;
    private decimal _estoque = 100m;
    private decimal _estoqueMinimo = 10m;
    private bool _controlaEstoque = true;
    private bool _permiteVendaFracionada = false;
    private string _ncm = "12345678";
    private string _cest = "2800000";
    private string _cfop = "5102";
    private string _origem = "0";
    private string _cstIcms = "102";
    private decimal _aliquotaIcms = 0.18m;
    private string _cstPisCofins = "49";

    public ProdutoBuilder WithId(int id)
    {
        _id = id;
        return this;
    }

    public ProdutoBuilder WithCodigo(string codigo)
    {
        _codigo = codigo;
        return this;
    }

    public ProdutoBuilder WithCodigoBarras(string codigoBarras)
    {
        _codigoBarras = codigoBarras;
        return this;
    }

    public ProdutoBuilder WithDescricao(string descricao)
    {
        _descricao = descricao;
        return this;
    }

    public ProdutoBuilder WithCategoriaId(int? categoriaId)
    {
        _categoriaId = categoriaId;
        return this;
    }

    public ProdutoBuilder WithUnidade(UnidadeMedida unidade)
    {
        _unidade = unidade;
        return this;
    }

    public ProdutoBuilder WithPrecoCusto(decimal precoCusto)
    {
        _precoCusto = precoCusto;
        return this;
    }

    public ProdutoBuilder WithPrecoVenda(decimal precoVenda)
    {
        _precoVenda = precoVenda;
        return this;
    }

    public ProdutoBuilder WithEstoque(decimal estoque)
    {
        _estoque = estoque;
        return this;
    }

    public ProdutoBuilder WithEstoqueMinimo(decimal estoqueMinimo)
    {
        _estoqueMinimo = estoqueMinimo;
        return this;
    }

    public ProdutoBuilder WithControlaEstoque(bool controlaEstoque)
    {
        _controlaEstoque = controlaEstoque;
        return this;
    }

    public ProdutoBuilder WithPermiteVendaFracionada(bool permite)
    {
        _permiteVendaFracionada = permite;
        return this;
    }

    public Produto Build()
    {
        return new Produto
        {
            Id = _id,
            Codigo = _codigo,
            CodigoBarras = _codigoBarras,
            Descricao = _descricao,
            CategoriaId = _categoriaId,
            Unidade = _unidade,
            PrecoCusto = _precoCusto,
            PrecoVenda = _precoVenda,
            Estoque = _estoque,
            EstoqueMinimo = _estoqueMinimo,
            ControlaEstoque = _controlaEstoque,
            PermiteVendaFracionada = _permiteVendaFracionada,
            Ncm = _ncm,
            Cest = _cest,
            Cfop = _cfop,
            Origem = _origem,
            CstIcms = _cstIcms,
            AliquotaIcms = _aliquotaIcms,
            CstPisCofins = _cstPisCofins
        };
    }

    /// <summary>
    /// Create a batch of products with auto-incrementing IDs and codes.
    /// </summary>
    public static List<Produto> CreateBatch(int count, string? codePrefix = null)
    {
        var products = new List<Produto>();
        for (int i = 1; i <= count; i++)
        {
            var code = codePrefix != null ? $"{codePrefix}{i:D4}" : $"PROD{i:D4}";
            var product = new ProdutoBuilder()
                .WithId(i)
                .WithCodigo(code)
                .WithCodigoBarras($"123456789{i:D3}")
                .WithDescricao($"Product {i}")
                .WithEstoque(100m * i)
                .WithPrecoVenda(10m * i)
                .Build();
            products.Add(product);
        }
        return products;
    }

    /// <summary>
    /// Create a simple product with minimal configuration.
    /// </summary>
    public static Produto CreateSimple(int id = 1, string codigo = "PROD001")
    {
        return new ProdutoBuilder()
            .WithId(id)
            .WithCodigo(codigo)
            .Build();
    }

    /// <summary>
    /// Create a product with low stock (below minimum).
    /// </summary>
    public static Produto CreateLowStock(int id = 1)
    {
        return new ProdutoBuilder()
            .WithId(id)
            .WithEstoque(5m)
            .WithEstoqueMinimo(10m)
            .Build();
    }

    /// <summary>
    /// Create a product with no stock.
    /// </summary>
    public static Produto CreateOutOfStock(int id = 1)
    {
        return new ProdutoBuilder()
            .WithId(id)
            .WithEstoque(0m)
            .Build();
    }
}
