using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class VendaService
{
    private readonly AppDbContext _db;
    private readonly SessaoApp _sessao;
    private readonly EstoqueService _estoque;

    public VendaService(AppDbContext db, SessaoApp sessao, EstoqueService estoque)
    {
        _db = db;
        _sessao = sessao;
        _estoque = estoque;
    }

    public async Task<Result<Venda>> NovaVendaAsync()
    {
        if (_sessao.UsuarioLogado == null)
            return Result.Falha<Venda>("Usuário não autenticado.");

        var numero = await GerarNumeroAsync();
        var venda = new Venda
        {
            Numero = numero,
            DataVenda = DateTime.Now,
            UsuarioId = _sessao.UsuarioLogado.Id,
            Status = StatusVenda.EmAberto
        };
        _db.Vendas.Add(venda);
        await _db.SaveChangesAsync();
        return Result.Ok(venda);
    }

    private async Task<string> GerarNumeroAsync()
    {
        var hoje = DateTime.Today.ToString("yyyyMMdd");
        var qtd = await _db.Vendas.CountAsync(v => v.Numero.StartsWith(hoje));
        return $"{hoje}{(qtd + 1):D4}";
    }

    public async Task<Result<ItemVenda>> AdicionarItemAsync(int vendaId, int produtoId, decimal quantidade, decimal? precoOverride = null)
    {
        if (quantidade <= 0) return Result.Falha<ItemVenda>("Quantidade inválida.");

        var venda = await _db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.Id == vendaId);
        if (venda == null) return Result.Falha<ItemVenda>("Venda não encontrada.");
        if (venda.Status != StatusVenda.EmAberto)
            return Result.Falha<ItemVenda>("Venda já finalizada/cancelada.");

        var produto = await _db.Produtos.FindAsync(produtoId);
        if (produto == null) return Result.Falha<ItemVenda>("Produto não encontrado.");
        if (!produto.Ativo) return Result.Falha<ItemVenda>("Produto inativo.");

        if (produto.ControlaEstoque && produto.Estoque < quantidade)
            return Result.Falha<ItemVenda>($"Estoque insuficiente. Disponível: {produto.Estoque}.");

        var preco = precoOverride ?? produto.PrecoVenda;
        var item = new ItemVenda
        {
            VendaId = vendaId,
            ProdutoId = produtoId,
            Quantidade = quantidade,
            PrecoUnitario = preco,
            Total = preco * quantidade
        };
        _db.ItensVenda.Add(item);
        await _db.SaveChangesAsync();
        await RecalcularTotaisAsync(vendaId);
        return Result.Ok(item);
    }

    public async Task<Result> RemoverItemAsync(int itemId)
    {
        var item = await _db.ItensVenda.FindAsync(itemId);
        if (item == null) return Result.Falha("Item não encontrado.");
        var vendaId = item.VendaId;
        var venda = await _db.Vendas.FindAsync(vendaId);
        if (venda?.Status != StatusVenda.EmAberto)
            return Result.Falha("Venda já finalizada.");
        _db.ItensVenda.Remove(item);
        await _db.SaveChangesAsync();
        await RecalcularTotaisAsync(vendaId);
        return Result.Ok();
    }

    public async Task RecalcularTotaisAsync(int vendaId)
    {
        var venda = await _db.Vendas.Include(v => v.Itens).FirstAsync(v => v.Id == vendaId);
        venda.SubTotal = venda.Itens.Sum(i => i.Total);
        venda.Total = venda.SubTotal - venda.Desconto + venda.Acrescimo;
        venda.AtualizadoEm = DateTime.Now;
        await _db.SaveChangesAsync();
    }

    public async Task<Result> AplicarDescontoAsync(int vendaId, decimal desconto)
    {
        var venda = await _db.Vendas.Include(v => v.Itens).FirstOrDefaultAsync(v => v.Id == vendaId);
        if (venda == null) return Result.Falha("Venda não encontrada.");
        if (desconto < 0 || desconto > venda.SubTotal)
            return Result.Falha("Desconto inválido.");
        venda.Desconto = desconto;
        await RecalcularTotaisAsync(vendaId);
        return Result.Ok();
    }

    public async Task<Result<Venda>> FinalizarAsync(int vendaId, List<PagamentoVenda> pagamentos, int? clienteId = null)
    {
        var venda = await _db.Vendas
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == vendaId);

        if (venda == null) return Result.Falha<Venda>("Venda não encontrada.");
        if (venda.Status != StatusVenda.EmAberto) return Result.Falha<Venda>("Venda já finalizada.");
        if (!venda.Itens.Any()) return Result.Falha<Venda>("Venda sem itens.");

        var totalPago = pagamentos.Sum(p => p.Valor);
        if (totalPago < venda.Total)
            return Result.Falha<Venda>($"Valor pago ({totalPago:C}) menor que o total ({venda.Total:C}).");

        using var tx = await _db.Database.BeginTransactionAsync();
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
                        await tx.RollbackAsync();
                        return Result.Falha<Venda>(res.Erro!);
                    }
                }
            }

            foreach (var p in pagamentos)
            {
                p.VendaId = venda.Id;
                _db.PagamentosVenda.Add(p);
            }

            venda.ClienteId = clienteId;
            venda.ValorPago = totalPago;
            venda.Troco = totalPago - venda.Total;
            venda.Status = StatusVenda.Finalizada;
            venda.FinalizadaEm = DateTime.Now;
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Result.Ok(venda);
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Result.Falha<Venda>($"Erro ao finalizar: {ex.Message}");
        }
    }

    public async Task<Result> CancelarAsync(int vendaId, string motivo)
    {
        var venda = await _db.Vendas
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == vendaId);
        if (venda == null) return Result.Falha("Venda não encontrada.");
        if (venda.Status == StatusVenda.Cancelada) return Result.Falha("Venda já cancelada.");

        using var tx = await _db.Database.BeginTransactionAsync();
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
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Result.Ok();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return Result.Falha($"Erro: {ex.Message}");
        }
    }

    public Task<Venda?> BuscarAsync(int id) =>
        _db.Vendas
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .Include(v => v.Pagamentos)
            .Include(v => v.Cliente)
            .Include(v => v.Usuario)
            .FirstOrDefaultAsync(v => v.Id == id);

    public Task<List<Venda>> ListarAsync(DateTime? de = null, DateTime? ate = null, StatusVenda? status = null)
    {
        var q = _db.Vendas.Include(v => v.Cliente).AsQueryable();
        if (de.HasValue) q = q.Where(v => v.DataVenda >= de.Value);
        if (ate.HasValue) q = q.Where(v => v.DataVenda <= ate.Value);
        if (status.HasValue) q = q.Where(v => v.Status == status.Value);
        return q.OrderByDescending(v => v.DataVenda).Take(500).ToListAsync();
    }
}
