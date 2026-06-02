namespace ProjetoVarejo.Infrastructure.Pix;

public enum PixStatus { Ativo, Concluido, Removido, Erro }

public class PixCobranca
{
    public string TxId { get; set; } = "";
    public string EndToEndId { get; set; } = "";
    public decimal Valor { get; set; }
    public PixStatus Status { get; set; }
    public string BrCode { get; set; } = "";
    public string? LocationId { get; set; }
    public string? Mensagem { get; set; }
    public DateTime QuandoCriado { get; set; } = DateTime.Now;
    public DateTime ExpiraEm { get; set; } = DateTime.Now.AddMinutes(30);
}

/// <summary>
/// Provider de PIX dinâmico (com confirmação real). Cada banco implementa.
/// </summary>
public interface IPixDinamicoProvider
{
    string Nome { get; }
    Task<PixCobranca> CriarCobrancaAsync(decimal valor, string descricao, int expiraSegundos = 1800);
    Task<PixStatus> ConsultarStatusAsync(string txId);
}
