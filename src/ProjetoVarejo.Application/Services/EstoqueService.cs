using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Application.Logging;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Serilog;

namespace ProjetoVarejo.Application.Services;

public class EstoqueService : IEstoqueService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SessaoApp _sessao;

    public EstoqueService(IUnitOfWork unitOfWork, SessaoApp sessao)
    {
        _unitOfWork = unitOfWork;
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
        try
        {
            if (quantidade <= 0)
            {
                Log.Warning("Tentativa de registrar movimento com quantidade inválida: {Quantidade}", quantidade);
                return Result.Falha<MovimentoEstoque>("Quantidade deve ser maior que zero.");
            }

            if (_sessao.UsuarioLogado == null)
            {
                Log.Warning("Tentativa de registrar movimento de estoque sem usuário autenticado");
                return Result.Falha<MovimentoEstoque>("Usuário não autenticado.");
            }

            var produto = await _unitOfWork.Produtos.GetByIdAsync(produtoId);
            if (produto == null)
            {
                Log.Warning("Produto {ProdutoId} não encontrado para movimento de estoque", produtoId);
                return Result.Falha<MovimentoEstoque>("Produto não encontrado.");
            }

            bool isEntrada = tipo == TipoMovimentoEstoque.Entrada
                           || tipo == TipoMovimentoEstoque.AjusteEntrada
                           || tipo == TipoMovimentoEstoque.Devolucao;

            var saldoAnterior = produto.Estoque;
            var saldoNovo = isEntrada ? saldoAnterior + quantidade : saldoAnterior - quantidade;

            if (!isEntrada && produto.ControlaEstoque && saldoNovo < 0)
            {
                Log.Warning(LogTemplates.EstoqueInsuficiente, produtoId, quantidade, saldoAnterior);
                return Result.Falha<MovimentoEstoque>(
                    $"Estoque insuficiente. Disponível: {saldoAnterior}, solicitado: {quantidade}.");
            }

            // Verificar se está abaixo do mínimo após movimento
            if (saldoNovo <= produto.EstoqueMinimo && produto.ControlaEstoque)
            {
                Log.Warning(LogTemplates.EstoqueAbaixoMinimo, produtoId, produto.Descricao, saldoNovo, produto.EstoqueMinimo);
            }

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
                await _unitOfWork.MovimentosEstoque.InsertAsync(mov);
                await _unitOfWork.SaveChangesAsync();

                Log.Information(LogTemplates.MovimentoEstoque, produtoId, tipo.ToString(), quantidade);
                return Result.Ok(mov);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Outro usuário modificou o mesmo produto. Carrega dados atualizados do banco
                produto = await _unitOfWork.Produtos.GetByIdAsync(produtoId);
                if (produto == null)
                {
                    Log.Error("Produto {ProdutoId} não encontrado após conflito de concorrência", produtoId);
                    return Result.Falha<MovimentoEstoque>("Produto não encontrado após conflito.");
                }

                Log.Warning("Conflito de concorrência ao registrar movimento para produto {ProdutoId}. Saldo: {Saldo}", produtoId, produto.Estoque);
                return Result.Falha<MovimentoEstoque>(
                    $"Conflito de concorrência: o estoque foi modificado por outro usuário. " +
                    $"Saldo atual: {produto.Estoque}. Tente novamente.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "EstoqueService.RegistrarMovimentoAsync", ex.Message);
            return Result.Falha<MovimentoEstoque>($"Erro ao registrar movimento: {ex.Message}");
        }
    }

    public Task<List<MovimentoEstoque>> ListarMovimentosAsync(int? produtoId = null, DateTime? de = null, DateTime? ate = null)
    {
        var q = _unitOfWork.MovimentosEstoque.Query()
            .Include(m => m.Produto)
            .Include(m => m.Usuario)
            .AsQueryable();
        if (produtoId.HasValue) q = q.Where(m => m.ProdutoId == produtoId.Value);
        if (de.HasValue) q = q.Where(m => m.CriadoEm >= de.Value);
        if (ate.HasValue) q = q.Where(m => m.CriadoEm <= ate.Value);
        return q.OrderByDescending(m => m.CriadoEm).Take(1000).ToListAsync();
    }

    public async Task<List<Produto>> ProdutosAbaixoMinimoAsync()
    {
        try
        {
            var produtos = await _unitOfWork.Produtos.Query()
                .Where(p => p.Ativo && p.ControlaEstoque && p.Estoque <= p.EstoqueMinimo)
                .OrderBy(p => p.Descricao)
                .ToListAsync();

            if (produtos.Count > 0)
            {
                Log.Warning("Total de {Quantidade} produtos abaixo do estoque mínimo detectados", produtos.Count);
            }

            return produtos;
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.ErroNaoTratado, "EstoqueService.ProdutosAbaixoMinimoAsync", ex.Message);
            return new List<Produto>();
        }
    }
}
