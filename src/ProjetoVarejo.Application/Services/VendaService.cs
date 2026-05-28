using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Logging;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Serilog;

namespace ProjetoVarejo.Application.Services;

public class VendaService : IVendaService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SessaoApp _sessao;
    private readonly EstoqueService _estoque;
    private readonly IValidator<Venda> _vendaValidator;
    private readonly IValidator<ItemVenda> _itemValidator;
    private readonly IValidator<PagamentoVenda> _pagamentoValidator;

    public VendaService(
        IUnitOfWork unitOfWork,
        SessaoApp sessao,
        EstoqueService estoque,
        IValidator<Venda> vendaValidator,
        IValidator<ItemVenda> itemValidator,
        IValidator<PagamentoVenda> pagamentoValidator)
    {
        _unitOfWork = unitOfWork;
        _sessao = sessao;
        _estoque = estoque;
        _vendaValidator = vendaValidator;
        _itemValidator = itemValidator;
        _pagamentoValidator = pagamentoValidator;
    }

    public async Task<Result<Venda>> NovaVendaAsync()
    {
        try
        {
            if (_sessao.UsuarioLogado == null)
            {
                Log.Warning("Tentativa de nova venda sem usuário autenticado");
                return Result.Falha<Venda>("Usuário não autenticado.");
            }

            Log.Information(LogTemplates.VendaIniciada, null, _sessao.UsuarioLogado.Nome);

            var numero = await GerarNumeroAsync();
            var venda = new Venda
            {
                Numero = numero,
                DataVenda = DateTime.Now,
                UsuarioId = _sessao.UsuarioLogado.Id,
                Status = StatusVenda.EmAberto
            };
            await _unitOfWork.Vendas.InsertAsync(venda);
            await _unitOfWork.SaveChangesAsync();

            Log.Information(LogTemplates.VendaCriadaComSucesso, venda.Id, venda.Numero);
            return Result.Ok(venda);
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "VendaService.NovaVendaAsync", ex.Message);
            return Result.Falha<Venda>($"Erro ao criar venda: {ex.Message}");
        }
    }

    private async Task<string> GerarNumeroAsync()
    {
        var hoje = DateTime.Today.ToString("yyyyMMdd");
        var qtd = await _unitOfWork.Vendas.Query().CountAsync(v => v.Numero.StartsWith(hoje));
        return $"{hoje}{(qtd + 1):D4}";
    }

    public async Task<Result<ItemVenda>> AdicionarItemAsync(int vendaId, int produtoId, decimal quantidade, decimal? precoOverride = null)
    {
        try
        {
            var venda = await _unitOfWork.Vendas.Query().Include(v => v.Itens).FirstOrDefaultAsync(v => v.Id == vendaId);
            if (venda == null)
            {
                Log.Warning("Tentativa de adicionar item a venda inexistente {VendaId}", vendaId);
                return Result.Falha<ItemVenda>("Venda não encontrada.");
            }

            if (venda.Status != StatusVenda.EmAberto)
            {
                Log.Warning("Tentativa de adicionar item a venda finalizada/cancelada {VendaId}", vendaId);
                return Result.Falha<ItemVenda>("Venda já finalizada/cancelada.");
            }

            var produto = await _unitOfWork.Produtos.GetByIdAsync(produtoId);
            if (produto == null)
            {
                Log.Warning("Produto {ProdutoId} não encontrado para venda {VendaId}", produtoId, vendaId);
                return Result.Falha<ItemVenda>("Produto não encontrado.");
            }

            if (!produto.Ativo)
            {
                Log.Warning(LogTemplates.ProdutoInativo, produtoId, produto.Codigo);
                return Result.Falha<ItemVenda>("Produto inativo.");
            }

            if (produto.ControlaEstoque && produto.Estoque < quantidade)
            {
                Log.Warning(LogTemplates.EstoqueInsuficiente, produtoId, quantidade, produto.Estoque);
                return Result.Falha<ItemVenda>($"Estoque insuficiente. Disponível: {produto.Estoque}.");
            }

            var preco = precoOverride ?? produto.PrecoVenda;
            var item = new ItemVenda
            {
                VendaId = vendaId,
                ProdutoId = produtoId,
                Quantidade = quantidade,
                PrecoUnitario = preco,
                Total = preco * quantidade,
                Desconto = 0
            };

            // Validar item antes de salvar
            var validationResult = await _itemValidator.ValidateAsync(item);
            if (!validationResult.IsValid)
            {
                var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
                Log.Warning(LogTemplates.ValidacaoFalhou, "ItemVenda", errors);
                return Result.Falha<ItemVenda>($"Erro de validação: {errors}");
            }

            await _unitOfWork.ItensVenda.InsertAsync(item);
            await _unitOfWork.SaveChangesAsync();
            await RecalcularTotaisAsync(vendaId);

            Log.Information(LogTemplates.ItemAdicionado, vendaId, produtoId, quantidade, preco);
            return Result.Ok(item);
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "VendaService.AdicionarItemAsync", ex.Message);
            return Result.Falha<ItemVenda>($"Erro ao adicionar item: {ex.Message}");
        }
    }

    public async Task<Result> RemoverItemAsync(int itemId)
    {
        var item = await _unitOfWork.ItensVenda.GetByIdAsync(itemId);
        if (item == null) return Result.Falha("Item não encontrado.");
        var vendaId = item.VendaId;
        var venda = await _unitOfWork.Vendas.GetByIdAsync(vendaId);
        if (venda?.Status != StatusVenda.EmAberto)
            return Result.Falha("Venda já finalizada.");
        await _unitOfWork.ItensVenda.DeleteAsync(itemId);
        await _unitOfWork.SaveChangesAsync();
        await RecalcularTotaisAsync(vendaId);
        return Result.Ok();
    }

    public async Task RecalcularTotaisAsync(int vendaId)
    {
        var venda = await _unitOfWork.Vendas.Query().Include(v => v.Itens).FirstAsync(v => v.Id == vendaId);
        venda.SubTotal = venda.Itens.Sum(i => i.Total);
        venda.Total = venda.SubTotal - venda.Desconto + venda.Acrescimo;
        venda.AtualizadoEm = DateTime.Now;
        await _unitOfWork.Vendas.UpdateAsync(venda);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<Result> AplicarDescontoAsync(int vendaId, decimal desconto)
    {
        var venda = await _unitOfWork.Vendas.Query().Include(v => v.Itens).FirstOrDefaultAsync(v => v.Id == vendaId);
        if (venda == null) return Result.Falha("Venda não encontrada.");
        if (desconto < 0 || desconto > venda.SubTotal)
            return Result.Falha("Desconto inválido.");
        venda.Desconto = desconto;
        await RecalcularTotaisAsync(vendaId);
        return Result.Ok();
    }

    public async Task<Result<Venda>> FinalizarAsync(int vendaId, List<PagamentoVenda> pagamentos, int? clienteId = null)
    {
        using var context = new StructuredLogContext(Guid.NewGuid().ToString(), _sessao.UsuarioLogado?.Nome ?? "Sistema", $"Finalizando venda {vendaId}");

        try
        {
            Log.Information(LogTemplates.TransacaoIniciada, "FinalizarVenda");

            var venda = await _unitOfWork.Vendas.Query()
                .Include(v => v.Itens).ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(v => v.Id == vendaId);

            if (venda == null)
            {
                Log.Warning("Tentativa de finalizar venda inexistente {VendaId}", vendaId);
                return Result.Falha<Venda>("Venda não encontrada.");
            }

            if (venda.Status != StatusVenda.EmAberto)
            {
                Log.Warning("Tentativa de finalizar venda já finalizada/cancelada {VendaId}", vendaId);
                return Result.Falha<Venda>("Venda já finalizada.");
            }

            if (!venda.Itens.Any())
            {
                Log.Warning("Tentativa de finalizar venda sem itens {VendaId}", vendaId);
                return Result.Falha<Venda>("Venda sem itens.");
            }

            // Validar venda antes de finalizar
            var vendaValidationResult = await _vendaValidator.ValidateAsync(venda);
            if (!vendaValidationResult.IsValid)
            {
                var errors = string.Join("; ", vendaValidationResult.Errors.Select(e => e.ErrorMessage));
                Log.Warning(LogTemplates.ValidacaoFalhou, "Venda", errors);
                return Result.Falha<Venda>($"Erro de validação: {errors}");
            }

            // Validar cada pagamento
            foreach (var pagamento in pagamentos)
            {
                var pagamentoValidationResult = await _pagamentoValidator.ValidateAsync(pagamento);
                if (!pagamentoValidationResult.IsValid)
                {
                    var errors = string.Join("; ", pagamentoValidationResult.Errors.Select(e => e.ErrorMessage));
                    Log.Warning("Validação falhou para pagamento: {Erros}", errors);
                    return Result.Falha<Venda>($"Erro no pagamento: {errors}");
                }
            }

            var totalPago = pagamentos.Sum(p => p.Valor);
            if (totalPago < venda.Total)
            {
                Log.Warning("Valor pago insuficiente para venda {VendaId}: {ValorPago} < {Total}", vendaId, totalPago, venda.Total);
                return Result.Falha<Venda>($"Valor pago ({totalPago:C}) menor que o total ({venda.Total:C}).");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                foreach (var item in venda.Itens)
                {
                    if (item.Produto.ControlaEstoque)
                    {
                        var res = await _estoque.RegistrarMovimentoAsync(
                            item.ProdutoId, TipoMovimentoEstoque.Saida,
                            item.Quantidade, null, $"VENDA {venda.Numero}", venda.Id);
                        if (!res.Sucesso)
                        {
                            Log.Error("Erro ao registrar movimento de estoque para venda {VendaId}: {Erro}", vendaId, res.Erro);
                            await _unitOfWork.RollbackAsync();
                            return Result.Falha<Venda>(res.Erro!);
                        }
                    }
                }

                foreach (var p in pagamentos)
                {
                    p.VendaId = venda.Id;
                    await _unitOfWork.PagamentosVenda.InsertAsync(p);
                }

                venda.ClienteId = clienteId;
                venda.ValorPago = totalPago;
                venda.Troco = totalPago - venda.Total;
                venda.Status = StatusVenda.Finalizada;
                venda.FinalizadaEm = DateTime.Now;
                await _unitOfWork.CommitAsync();

                Log.Information(LogTemplates.VendaFinalizada, vendaId, venda.Total, venda.ValorPago, venda.Troco);
                Log.Information(LogTemplates.TransacaoConfirmada, "FinalizarVenda");
                return Result.Ok(venda);
            }
            catch (Exception ex)
            {
                Log.Error(ex, LogTemplates.TransacaoFalhou, "FinalizarVenda", ex.Message);
                await _unitOfWork.RollbackAsync();
                return Result.Falha<Venda>($"Erro ao finalizar: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "VendaService.FinalizarAsync", ex.Message);
            return Result.Falha<Venda>($"Erro: {ex.Message}");
        }
    }

    public async Task<Result> CancelarAsync(int vendaId, string motivo)
    {
        using var context = new StructuredLogContext(Guid.NewGuid().ToString(), _sessao.UsuarioLogado?.Nome ?? "Sistema", $"Cancelando venda {vendaId}");

        try
        {
            Log.Information(LogTemplates.TransacaoIniciada, "CancelarVenda");

            var venda = await _unitOfWork.Vendas.Query()
                .Include(v => v.Itens).ThenInclude(i => i.Produto)
                .FirstOrDefaultAsync(v => v.Id == vendaId);

            if (venda == null)
            {
                Log.Warning("Tentativa de cancelar venda inexistente {VendaId}", vendaId);
                return Result.Falha("Venda não encontrada.");
            }

            if (venda.Status == StatusVenda.Cancelada)
            {
                Log.Warning("Tentativa de cancelar venda já cancelada {VendaId}", vendaId);
                return Result.Falha("Venda já cancelada.");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (venda.Status == StatusVenda.Finalizada)
                {
                    foreach (var item in venda.Itens)
                    {
                        if (item.Produto.ControlaEstoque)
                        {
                            await _estoque.RegistrarMovimentoAsync(
                                item.ProdutoId, TipoMovimentoEstoque.Devolucao,
                                item.Quantidade, null, $"CANCEL VENDA {venda.Numero}", venda.Id,
                                observacao: motivo);
                        }
                    }
                }

                venda.Status = StatusVenda.Cancelada;
                venda.CanceladaEm = DateTime.Now;
                venda.Observacao = motivo;
                await _unitOfWork.Vendas.UpdateAsync(venda);
                await _unitOfWork.CommitAsync();

                Log.Information(LogTemplates.VendaCancelada, vendaId, motivo);
                Log.Information(LogTemplates.TransacaoConfirmada, "CancelarVenda");
                return Result.Ok();
            }
            catch (Exception ex)
            {
                Log.Error(ex, LogTemplates.TransacaoFalhou, "CancelarVenda", ex.Message);
                await _unitOfWork.RollbackAsync();
                return Result.Falha($"Erro: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "VendaService.CancelarAsync", ex.Message);
            return Result.Falha($"Erro: {ex.Message}");
        }
    }

    public Task<Venda?> BuscarAsync(int id) =>
        _unitOfWork.Vendas.Query()
            .AsNoTracking()
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.Cliente)
            .Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == id);

    public Task<List<Venda>> ListarAsync(DateTime? de = null, DateTime? ate = null, StatusVenda? status = null)
    {
        var q = _unitOfWork.Vendas.Query().AsNoTracking().Include(v => v.Cliente).AsQueryable();
        if (de.HasValue) q = q.Where(v => v.DataVenda >= de.Value);
        if (ate.HasValue) q = q.Where(v => v.DataVenda <= ate.Value);
        if (status.HasValue) q = q.Where(v => v.Status == status.Value);
        return q.OrderByDescending(v => v.DataVenda).Take(500).ToListAsync();
    }
}
