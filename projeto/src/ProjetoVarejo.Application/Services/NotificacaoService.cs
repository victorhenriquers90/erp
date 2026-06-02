using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Infrastructure.WhatsApp;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class NotificacaoService
{
    private readonly AppDbContext _db;
    private readonly WhatsAppService _whatsApp;

    public NotificacaoService(AppDbContext db, WhatsAppService whatsApp)
    {
        _db = db;
        _whatsApp = whatsApp;
    }

    public async Task<bool> EnviarConfirmacaoVendaAsync(int vendaId)
    {
        var venda = await _db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.NotaFiscal)
            .FirstOrDefaultAsync(v => v.Id == vendaId);

        if (venda?.Cliente?.Telefone == null)
            return false;

        var nfce = venda.NotaFiscal?.ChaveAcesso ?? venda.Numero.ToString();
        var resultado = await _whatsApp.EnviarConfirmacaoVendaAsync(
            venda.Cliente.Telefone,
            venda.Cliente.Nome,
            venda.Total,
            nfce);

        return resultado.Sucesso;
    }

    public async Task<int> EnviarCobrancasVencidasAsync()
    {
        var contas = await _db.ContasFinanceiras
            .Include(c => c.Cliente)
            .Include(c => c.Fornecedor)
            .Where(c => c.Status == StatusConta.Atrasada)
            .ToListAsync();

        int enviadas = 0;
        foreach (var conta in contas)
        {
            var telefone = conta.Cliente?.Telefone ?? conta.Fornecedor?.Telefone;
            var nome = conta.Cliente?.Nome ?? conta.Fornecedor?.RazaoSocial;
            if (telefone == null || nome == null) continue;

            var resultado = await _whatsApp.EnviarCobrancaVencidaAsync(
                telefone, nome, conta.Valor, conta.DataVencimento);

            if (resultado.Sucesso) enviadas++;
        }
        return enviadas;
    }

    public Task<WhatsAppResultado> EnviarPixAsync(string telefoneDestino, decimal valor, string brCode)
        => _whatsApp.EnviarPixCopiaECola(telefoneDestino, valor, brCode);
}
