namespace ProjetoVarejo.Infrastructure.Tef;

/// <summary>
/// Implementação simulada de TEF para desenvolvimento e demo.
/// Substitua por TefSiTef, TefStone etc em produção.
/// </summary>
public class TefSimulador : ITefService
{
    private readonly Dictionary<string, TefTransacao> _transacoes = new();
    public string Nome => "Simulador";

    public Task<TefTransacao> IniciarAsync(TefBandeira bandeira, decimal valor, int parcelas = 1)
    {
        // simula latência da maquininha
        Thread.Sleep(800);

        var tx = new TefTransacao
        {
            Bandeira = bandeira,
            Valor = valor,
            Parcelas = parcelas,
            Status = TefStatus.Pendente,
            CodigoRede = "SIMULADOR",
            Mensagem = "Aguardando confirmação..."
        };
        _transacoes[tx.Id] = tx;
        return Task.FromResult(tx);
    }

    public Task<TefTransacao> ConfirmarAsync(string idTransacao)
    {
        Thread.Sleep(500);
        if (!_transacoes.TryGetValue(idTransacao, out var tx))
            return Task.FromResult(new TefTransacao { Status = TefStatus.Erro, Mensagem = "Transação não encontrada." });

        tx.Status = TefStatus.Aprovado;
        tx.Nsu = Random.Shared.Next(100000, 999999).ToString();
        tx.Autorizacao = Random.Shared.Next(100000, 999999).ToString();
        tx.Mensagem = "APROVADO";
        tx.ComprovanteEstabelecimento = MontarComprovante(tx, "VIA ESTABELECIMENTO");
        tx.ComprovanteCliente = MontarComprovante(tx, "VIA CLIENTE");
        return Task.FromResult(tx);
    }

    public Task<TefTransacao> CancelarAsync(string idTransacao)
    {
        if (_transacoes.TryGetValue(idTransacao, out var tx))
        {
            tx.Status = TefStatus.Cancelado;
            tx.Mensagem = "Cancelado pelo operador";
        }
        return Task.FromResult(tx ?? new TefTransacao { Status = TefStatus.Cancelado });
    }

    private static string MontarComprovante(TefTransacao tx, string via)
    {
        return
$@"==========================
  COMPROVANTE TEF SIMULADO
  {via}
==========================
Data: {tx.QuandoOcorreu:dd/MM/yyyy HH:mm:ss}
Bandeira: {tx.Bandeira}
Valor: R$ {tx.Valor:N2}
Parcelas: {tx.Parcelas}x
NSU: {tx.Nsu}
Aut: {tx.Autorizacao}
Status: APROVADO
==========================
*** TRANSAÇÃO DEMO ***
   Sem valor real
==========================";
    }
}
