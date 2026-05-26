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

        // RowVersion garante que múltiplas operações simultâneas não causem inconsistência
        // DbUpdateConcurrencyException é lançada se outro usuário já modificou este produto
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

        try
        {
            _db.MovimentosEstoque.Add(mov);
            await _db.SaveChangesAsync();
            return Result.Ok(mov);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            // Outro usuário modificou o mesmo produto. Carrega dados atualizados do banco
            await _db.Entry(produto).ReloadAsync();

            return Result.Falha<MovimentoEstoque>(
                $"Conflito de concorrência: o estoque foi modificado por outro usuário. " +
                $"Saldo atual: {produto.Estoque}. Tente novamente.");
        }
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
