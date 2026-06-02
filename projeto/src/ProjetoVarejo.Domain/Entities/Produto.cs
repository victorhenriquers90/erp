using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Domain.Entities;

public class Produto : EntidadeBase
{
    public string Codigo { get; set; } = string.Empty;
    public string? CodigoBarras { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }
    public UnidadeMedida Unidade { get; set; } = UnidadeMedida.UN;
    public decimal PrecoCusto { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal Estoque { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public bool ControlaEstoque { get; set; } = true;
    public bool PermiteVendaFracionada { get; set; }

    public string? Ncm { get; set; }
    public string? Cest { get; set; }
    public string Cfop { get; set; } = "5102";
    public string Origem { get; set; } = "0";
    public string CstIcms { get; set; } = "102";
    public decimal AliquotaIcms { get; set; }
    public string CstPisCofins { get; set; } = "49";

    public byte[] RowVersion { get; set; } = [];

    public ICollection<ItemVenda> ItensVenda { get; set; } = new List<ItemVenda>();
    public ICollection<MovimentoEstoque> Movimentos { get; set; } = new List<MovimentoEstoque>();
}
