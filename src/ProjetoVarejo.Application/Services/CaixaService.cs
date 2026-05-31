using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Contracts.Services.DTOs;
using ProjetoVarejo.Application.Logging;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Serilog;

namespace ProjetoVarejo.Application.Services;

public class CaixaService : ICaixaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SessaoApp _sessao;
    private readonly IValidator<CaixaSessao> _caixaValidator;

    public CaixaService(IUnitOfWork unitOfWork, SessaoApp sessao, IValidator<CaixaSessao> caixaValidator)
    {
        _unitOfWork = unitOfWork;
        _sessao = sessao;
        _caixaValidator = caixaValidator;
    }

    public async Task<CaixaSessao?> ObterCaixaAbertoAsync()
    {
        if (_sessao.UsuarioLogado == null) return null;
        var aberto = await _unitOfWork.CaixaSessoes.Query()
            .Where(c => c.UsuarioAberturaId == _sessao.UsuarioLogado.Id && c.FechadaEm == null)
            .OrderByDescending(c => c.AbertaEm)
            .FirstOrDefaultAsync();
        if (aberto != null) _sessao.CaixaAtual = aberto;
        return aberto;
    }

    public async Task<Result<CaixaSessao>> AbrirAsync(decimal valorAbertura)
    {
        using var context = new StructuredLogContext(Guid.NewGuid().ToString(), _sessao.UsuarioLogado?.Nome ?? "Sistema", "Abrindo caixa");

        try
        {
            if (_sessao.UsuarioLogado == null)
            {
                Log.Warning("Tentativa de abrir caixa sem usuário autenticado");
                return Result.Falha<CaixaSessao>("Usuário não autenticado.");
            }

            var aberto = await ObterCaixaAbertoAsync();
            if (aberto != null)
            {
                Log.Warning(LogTemplates.CaixaJaAberto);
                return Result.Falha<CaixaSessao>("Já existe caixa aberto para este usuário.");
            }

            var caixa = new CaixaSessao
            {
                UsuarioAberturaId = _sessao.UsuarioLogado.Id,
                AbertaEm = DateTime.Now,
                ValorAbertura = valorAbertura
            };

            // Validar caixa antes de salvar
            var validationResult = await _caixaValidator.ValidateAsync(caixa);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                Log.Warning(LogTemplates.ValidacaoFalhou, "CaixaSessao", errors);
                return Result.Falha<CaixaSessao>($"Erro de validação: {errors}");
            }

            await _unitOfWork.CaixaSessoes.InsertAsync(caixa);
            await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.MovimentosCaixa.InsertAsync(new MovimentoCaixa
            {
                CaixaSessaoId = caixa.Id,
                Tipo = TipoMovimentoCaixa.Abertura,
                Valor = valorAbertura,
                FormaPagamento = FormaPagamentoTipo.Dinheiro,
                UsuarioId = _sessao.UsuarioLogado.Id,
                Observacao = "Abertura de caixa"
            });
            await _unitOfWork.SaveChangesAsync();
            _sessao.CaixaAtual = caixa;

            Log.Information(LogTemplates.CaixaAberto, caixa.Id, _sessao.UsuarioLogado.Nome, valorAbertura);
            return Result.Ok(caixa);
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "CaixaService.AbrirAsync", ex.Message);
            return Result.Falha<CaixaSessao>($"Erro ao abrir caixa: {ex.Message}");
        }
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
        try
        {
            if (_sessao.UsuarioLogado == null)
            {
                Log.Warning("Tentativa de registrar movimento sem usuário autenticado");
                return Result.Falha<MovimentoCaixa>("Usuário não autenticado.");
            }

            if (valor <= 0)
            {
                Log.Warning("Tentativa de registrar movimento com valor inválido: {Valor}", valor);
                return Result.Falha<MovimentoCaixa>("Valor inválido.");
            }

            if (string.IsNullOrWhiteSpace(motivo))
            {
                Log.Warning("Tentativa de registrar movimento sem motivo");
                return Result.Falha<MovimentoCaixa>("Informe o motivo.");
            }

            var caixa = await ObterCaixaAbertoAsync();
            if (caixa == null)
            {
                Log.Warning("Tentativa de registrar movimento sem caixa aberto");
                return Result.Falha<MovimentoCaixa>("Nenhum caixa aberto.");
            }

            var mov = new MovimentoCaixa
            {
                CaixaSessaoId = caixa.Id,
                Tipo = tipo,
                Valor = valor,
                FormaPagamento = FormaPagamentoTipo.Dinheiro,
                UsuarioId = _sessao.UsuarioLogado.Id,
                Observacao = motivo
            };

            await _unitOfWork.MovimentosCaixa.InsertAsync(mov);
            await _unitOfWork.SaveChangesAsync();

            if (tipo == TipoMovimentoCaixa.Sangria)
                Log.Information(LogTemplates.SangriaRegistrada, valor, caixa.Id, motivo);
            else if (tipo == TipoMovimentoCaixa.Suprimento)
                Log.Information(LogTemplates.SuprimentoRegistrado, valor, caixa.Id, motivo);

            return Result.Ok(mov);
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "CaixaService.RegistrarMovimentoAsync", ex.Message);
            return Result.Falha<MovimentoCaixa>($"Erro ao registrar movimento: {ex.Message}");
        }
    }

    public async Task<Result> RegistrarVendaAsync(int caixaId, int vendaId, IEnumerable<PagamentoVenda> pagamentos)
    {
        try
        {
            if (_sessao.UsuarioLogado == null)
            {
                Log.Warning("Tentativa de registrar venda no caixa sem usuário autenticado");
                return Result.Falha("Usuário não autenticado.");
            }

            foreach (var p in pagamentos)
            {
                await _unitOfWork.MovimentosCaixa.InsertAsync(new MovimentoCaixa
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

            await _unitOfWork.SaveChangesAsync();
            Log.Information(LogTemplates.VendaRegistradaEmCaixa, vendaId, caixaId, "Dinheiro");
            return Result.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "CaixaService.RegistrarVendaAsync", ex.Message);
            return Result.Falha($"Erro ao registrar venda: {ex.Message}");
        }
    }

    public async Task<ResumoCaixa> ResumoAsync(int caixaSessaoId)
    {
        var caixa = await _unitOfWork.CaixaSessoes.Query().FirstAsync(c => c.Id == caixaSessaoId);
        var movs = await _unitOfWork.MovimentosCaixa.Query()
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
        using var context = new StructuredLogContext(Guid.NewGuid().ToString(), _sessao.UsuarioLogado?.Nome ?? "Sistema", $"Fechando caixa {caixaSessaoId}");

        try
        {
            if (_sessao.UsuarioLogado == null)
            {
                Log.Warning("Tentativa de fechar caixa sem usuário autenticado");
                return Result.Falha<CaixaSessao>("Usuário não autenticado.");
            }

            var caixa = await _unitOfWork.CaixaSessoes.Query().FirstOrDefaultAsync(c => c.Id == caixaSessaoId);
            if (caixa == null)
            {
                Log.Warning("Tentativa de fechar caixa inexistente {CaixaId}", caixaSessaoId);
                return Result.Falha<CaixaSessao>("Caixa não encontrado.");
            }

            if (caixa.FechadaEm != null)
            {
                Log.Warning("Tentativa de fechar caixa já fechado {CaixaId}", caixaSessaoId);
                return Result.Falha<CaixaSessao>("Caixa já foi fechado.");
            }

            var resumo = await ResumoAsync(caixaSessaoId);
            caixa.ValorFechamentoInformado = valorInformado;
            caixa.ValorFechamentoCalculado = resumo.SaldoDinheiroEsperado;
            caixa.Diferenca = valorInformado - resumo.SaldoDinheiroEsperado;
            caixa.FechadaEm = DateTime.Now;
            caixa.UsuarioFechamentoId = _sessao.UsuarioLogado.Id;
            caixa.Observacao = observacao;

            await _unitOfWork.MovimentosCaixa.InsertAsync(new MovimentoCaixa
            {
                CaixaSessaoId = caixaSessaoId,
                Tipo = TipoMovimentoCaixa.Fechamento,
                Valor = valorInformado,
                FormaPagamento = FormaPagamentoTipo.Dinheiro,
                UsuarioId = _sessao.UsuarioLogado.Id,
                Observacao = $"Fechamento. Diferença: {caixa.Diferenca:C}"
            });

            await _unitOfWork.CaixaSessoes.UpdateAsync(caixa);
            await _unitOfWork.SaveChangesAsync();
            _sessao.CaixaAtual = null;

            Log.Information(LogTemplates.CaixaFechado, caixaSessaoId, caixa.ValorFechamentoCalculado, valorInformado, caixa.Diferenca);
            return Result.Ok(caixa);
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "CaixaService.FecharAsync", ex.Message);
            return Result.Falha<CaixaSessao>($"Erro ao fechar caixa: {ex.Message}");
        }
    }
}
