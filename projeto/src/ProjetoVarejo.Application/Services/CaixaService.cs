using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class CaixaService
{
    private readonly AppDbContext _db;
    private readonly SessaoApp _sessao;

    public CaixaService(AppDbContext db, SessaoApp sessao)
    {
        _db = db; _sessao = sessao;
    }

    public async Task<CaixaSessao?> ObterCaixaAbertoAsync()
    {
        if (_sessao.UsuarioLogado == null) return null;
        var aberto = await _db.CaixasSessao
            .Where(c => c.UsuarioAberturaId == _sessao.UsuarioLogado.Id && c.FechadaEm == null)
            .OrderByDescending(c => c.AbertaEm)
            .FirstOrDefaultAsync();
        if (aberto != null) _sessao.CaixaAtual = aberto;
        return aberto;
    }

    public async Task<Result<CaixaSessao>> AbrirAsync(decimal valorAbertura)
    {
        if (_sessao.UsuarioLogado == null) return Result.Falha<CaixaSessao>("Usuário não autenticado.");
        if (valorAbertura < 0) return Result.Falha<CaixaSessao>("Valor de abertura inválido.");

        var aberto = await ObterCaixaAbertoAsync();
        if (aberto != null) return Result.Falha<CaixaSessao>("Já existe caixa aberto para este usuário.");

        var caixa = new CaixaSessao
        {
            UsuarioAberturaId = _sessao.UsuarioLogado.Id,
            AbertaEm = DateTime.Now,
            ValorAbertura = valorAbertura
        };
        _db.CaixasSessao.Add(caixa);
        await _db.SaveChangesAsync();

        _db.MovimentosCaixa.Add(new MovimentoCaixa
        {
            CaixaSessaoId = caixa.Id,
            Tipo = TipoMovimentoCaixa.Abertura,
            Valor = valorAbertura,
            FormaPagamento = FormaPagamentoTipo.Dinheiro,
            UsuarioId = _sessao.UsuarioLogado.Id,
            Observacao = "Abertura de caixa"
        });
        await _db.SaveChangesAsync();
        _sessao.CaixaAtual = caixa;
        return Result.Ok(caixa);
    }

    public async Task<Result<MovimentoCaixa>> SangriaAsync(decimal valor, string motivo)
    {
        return await RegistrarMovimentoAsync(TipoMovimentoCaixa.Sangria, valor, motivo);
    }

    public async Task<Result<MovimentoCaixa>> SuprimentoAsync(decimal valor, string motivo)
    {
        return await RegistrarMovimentoAsync(TipoMovimentoCaixa.Suprimento, valor, motivo);
    }

    private async Task<Result<MovimentoCaixa>> RegistrarMovimentoAsync(TipoMovimentoCaixa tipo, decimal valor, string motivo)
    {
        if (_sessao.UsuarioLogado == null) return Result.Falha<MovimentoCaixa>("Usuário não autenticado.");
        if (valor <= 0) return Result.Falha<MovimentoCaixa>("Valor inválido.");
        if (string.IsNullOrWhiteSpace(motivo)) return Result.Falha<MovimentoCaixa>("Informe o motivo.");

        var caixa = await ObterCaixaAbertoAsync();
        if (caixa == null) return Result.Falha<MovimentoCaixa>("Nenhum caixa aberto.");

        var mov = new MovimentoCaixa
        {
            CaixaSessaoId = caixa.Id,
            Tipo = tipo,
            Valor = valor,
            FormaPagamento = FormaPagamentoTipo.Dinheiro,
            UsuarioId = _sessao.UsuarioLogado.Id,
            Observacao = motivo
        };
        _db.MovimentosCaixa.Add(mov);
        await _db.SaveChangesAsync();
        return Result.Ok(mov);
    }

    public async Task<Result> RegistrarVendaAsync(int caixaId, int vendaId, IEnumerable<PagamentoVenda> pagamentos)
    {
        if (_sessao.UsuarioLogado == null) return Result.Falha("Usuário não autenticado.");
        foreach (var p in pagamentos)
        {
            _db.MovimentosCaixa.Add(new MovimentoCaixa
            {
                CaixaSessaoId = caixaId,
                Tipo = TipoMovimentoCaixa.Venda,
                Valor = p.Valor,
                FormaPagamento = p.FormaPagamento,
                VendaId = vendaId,
                UsuarioId = _sessao.UsuarioLogado.Id,
                Observacao = $"Venda #{vendaId}"
            });
        }
        await _db.SaveChangesAsync();
        return Result.Ok();
    }

    public class ResumoCaixa
    {
        public decimal ValorAbertura { get; set; }
        public decimal TotalSuprimentos { get; set; }
        public decimal TotalSangrias { get; set; }
        public Dictionary<FormaPagamentoTipo, decimal> VendasPorForma { get; set; } = new();
        public decimal TotalVendas => VendasPorForma.Values.Sum();
        public decimal SaldoDinheiroEsperado =>
            ValorAbertura
            + TotalSuprimentos
            - TotalSangrias
            + (VendasPorForma.TryGetValue(FormaPagamentoTipo.Dinheiro, out var d) ? d : 0);
    }

    public async Task<ResumoCaixa> ResumoAsync(int caixaSessaoId)
    {
        var caixa = await _db.CaixasSessao.FirstAsync(c => c.Id == caixaSessaoId);
        var movs = await _db.MovimentosCaixa
            .Where(m => m.CaixaSessaoId == caixaSessaoId && m.Tipo != TipoMovimentoCaixa.Abertura && m.Tipo != TipoMovimentoCaixa.Fechamento)
            .ToListAsync();

        var resumo = new ResumoCaixa { ValorAbertura = caixa.ValorAbertura };
        resumo.TotalSuprimentos = movs.Where(m => m.Tipo == TipoMovimentoCaixa.Suprimento).Sum(m => m.Valor);
        resumo.TotalSangrias = movs.Where(m => m.Tipo == TipoMovimentoCaixa.Sangria).Sum(m => m.Valor);
        foreach (var g in movs.Where(m => m.Tipo == TipoMovimentoCaixa.Venda && m.FormaPagamento.HasValue)
                              .GroupBy(m => m.FormaPagamento!.Value))
            resumo.VendasPorForma[g.Key] = g.Sum(m => m.Valor);
        return resumo;
    }

    public async Task<Result<CaixaSessao>> FecharAsync(int caixaSessaoId, decimal valorInformado, string? observacao = null)
    {
        if (_sessao.UsuarioLogado == null) return Result.Falha<CaixaSessao>("Usuário não autenticado.");
        var caixa = await _db.CaixasSessao.FirstOrDefaultAsync(c => c.Id == caixaSessaoId);
        if (caixa == null) return Result.Falha<CaixaSessao>("Caixa não encontrado.");
        if (caixa.FechadaEm != null) return Result.Falha<CaixaSessao>("Caixa já foi fechado.");

        var resumo = await ResumoAsync(caixaSessaoId);
        caixa.ValorFechamentoInformado = valorInformado;
        caixa.ValorFechamentoCalculado = resumo.SaldoDinheiroEsperado;
        caixa.Diferenca = valorInformado - resumo.SaldoDinheiroEsperado;
        caixa.FechadaEm = DateTime.Now;
        caixa.UsuarioFechamentoId = _sessao.UsuarioLogado.Id;
        caixa.Observacao = observacao;

        _db.MovimentosCaixa.Add(new MovimentoCaixa
        {
            CaixaSessaoId = caixaSessaoId,
            Tipo = TipoMovimentoCaixa.Fechamento,
            Valor = valorInformado,
            FormaPagamento = FormaPagamentoTipo.Dinheiro,
            UsuarioId = _sessao.UsuarioLogado.Id,
            Observacao = $"Fechamento. Diferença: {caixa.Diferenca:C}"
        });
        await _db.SaveChangesAsync();
        _sessao.CaixaAtual = null;
        return Result.Ok(caixa);
    }
}
