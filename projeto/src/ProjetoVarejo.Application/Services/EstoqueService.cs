using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class EstoqueService
{
    private readonly AppDbContext _db;
    private readonly SessaoApp _sessao;

    public EstoqueService(AppDbContext db, SessaoApp sessao)
    {
        _db = db;
        _sessao = sessao;
    }

    public async Task<Result<MovimentoEstoque>> RegistrarMovimentoAsync(
        int produtoId,
        TipoMovimentoEstoque tipo,
        decimal quantidade,
        decimal? custoUnitario = null,
        string? documento = null,
        int? vendaId = null,
        int? fornecedorId = null,
        string? observacao = null)
    {
        if (quantidade <= 0)
            return Result.Falha<MovimentoEstoque>("Quantidade deve ser maior que zero.");
        if (_sessao.UsuarioLogado == null)
            return Result.Falha<MovimentoEstoque>("Usuário não autenticado.");

        var produto = await _db.Produtos.FindAsync(produtoId);
        if (produto == null) return Result.Falha<MovimentoEstoque>("Produto não encontrado.");

        bool isEntrada = tipo == TipoMovimentoEstoque.Entrada
                       || tipo == TipoMovimentoEstoque.AjusteEntrada
                       || tipo == TipoMovimentoEstoque.Devolucao;

        var saldoAnterior = produto.Estoque;
        var saldoNovo = isEntrada ? saldoAnterior + quantidade : saldoAnterior - quantidade;

        if (!isEntrada && produto.ControlaEstoque && saldoNovo < 0)
            return Result.Falha<MovimentoEstoque>(
                $"Estoque insuficiente. Disponível: {saldoAnterior}, solicitado: {quantidade}.");

        produto.Estoque = saldoNovo;
        produto.AtualizadoEm = DateTime.Now;
        if (isEntrada && custoUnitario.HasValue && custoUnitario.Value > 0)
            produto.PrecoCusto = custoUnitario.Value;

        var mov = new MovimentoEstoque
        {
            ProdutoId = produtoId,
            Tipo = tipo,
            Quantidade = quantidade,
            SaldoAnterior = saldoAnterior,
            SaldoAtual = saldoNovo,
            CustoUnitario = custoUnitario,
            Documento = documento,
            VendaId = vendaId,
            FornecedorId = fornecedorId,
            UsuarioId = _sessao.UsuarioLogado.Id,
            Observacao = observacao
        };

        _db.MovimentosEstoque.Add(mov);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            return Result.Falha<MovimentoEstoque>("Conflito de estoque detectado (multi-caixa). Tente novamente.");
        }
        return Result.Ok(mov);
    }

    public Task<List<MovimentoEstoque>> ListarMovimentosAsync(int? produtoId = null, DateTime? de = null, DateTime? ate = null)
    {
        var q = _db.MovimentosEstoque
            .Include(m => m.Produto)
            .Include(m => m.Usuario)
            .AsQueryable();
        if (produtoId.HasValue) q = q.Where(m => m.ProdutoId == produtoId.Value);
        if (de.HasValue) q = q.Where(m => m.CriadoEm >= de.Value);
        if (ate.HasValue) q = q.Where(m => m.CriadoEm <= ate.Value);
        return q.OrderByDescending(m => m.CriadoEm).Take(1000).ToListAsync();
    }

    public Task<List<Produto>> ProdutosAbaixoMinimoAsync() =>
        _db.Produtos
            .Where(p => p.Ativo && p.ControlaEstoque && p.Estoque <= p.EstoqueMinimo)
            .OrderBy(p => p.Descricao)
            .ToListAsync();
}
