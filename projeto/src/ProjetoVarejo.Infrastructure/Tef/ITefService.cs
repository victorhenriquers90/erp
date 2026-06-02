namespace ProjetoVarejo.Infrastructure.Tef;

public enum TefBandeira { Credito, Debito, Voucher, Pix }
public enum TefStatus { Aprovado, Negado, Cancelado, Pendente, Erro }

public class TefTransacao
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    public TefBandeira Bandeira { get; set; }
    public decimal Valor { get; set; }
    public int Parcelas { get; set; } = 1;
    public TefStatus Status { get; set; }
    public string? Nsu { get; set; }            // Número Sequencial Único (do banco)
    public string? Autorizacao { get; set; }    // Código autorização
    public string? CodigoRede { get; set; }     // Rede adquirente (ex: STONE, CIELO)
    public string? Mensagem { get; set; }
    public string? ComprovanteEstabelecimento { get; set; }
    public string? ComprovanteCliente { get; set; }
    public DateTime QuandoOcorreu { get; set; } = DateTime.Now;
}

/// <summary>
/// Abstração de TEF (Transferência Eletrônica de Fundos). Permite plugar SiTef, Stone, etc.
/// </summary>
public interface ITefService
{
    string Nome { get; }
    Task<TefTransacao> IniciarAsync(TefBandeira bandeira, decimal valor, int parcelas = 1);
    Task<TefTransacao> ConfirmarAsync(string idTransacao);
    Task<TefTransacao> CancelarAsync(string idTransacao);
}
