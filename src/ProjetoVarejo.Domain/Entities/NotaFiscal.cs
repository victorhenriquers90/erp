using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Domain.Entities;

public class NotaFiscal : EntidadeBase
{
    public int Numero { get; set; }
    public int Serie { get; set; } = 1;
    public string Modelo { get; set; } = "65";
    public string? ChaveAcesso { get; set; }
    public string? Protocolo { get; set; }
    public DateTime? AutorizadaEm { get; set; }
    public StatusNotaFiscal Status { get; set; } = StatusNotaFiscal.NaoEmitida;
    public string? XmlEnviado { get; set; }
    public string? XmlRetorno { get; set; }
    public string? MensagemSefaz { get; set; }
    public string? JustificativaCancelamento { get; set; }
    public DateTime? CanceladaEm { get; set; }
    public bool EmitidaEmContingencia { get; set; }
    public DateTime? ReenviadaEm { get; set; }
    public int VendaId { get; set; }
    public Venda Venda { get; set; } = null!;
}
