using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Application.Services;

public class PedidoCompraService
{
    private readonly IUnitOfWork _unitOfWork;

    public PedidoCompraService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PedidoCompra>> ListarAsync(string? status = null, DateTime? de = null, DateTime? ate = null)
    {
        var query = _unitOfWork.PedidosCompra.Query()
            .Include(p => p.Fornecedor)
            .Include(p => p.Itens)
            .Where(p => p.Ativo);

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("Todos", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Status == status);
        }

        if (de.HasValue)
        {
            var dataInicial = de.Value.Date;
            query = query.Where(p => p.DataEmissao >= dataInicial);
        }

        if (ate.HasValue)
        {
            var dataFinal = ate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(p => p.DataEmissao <= dataFinal);
        }

        return await query.OrderByDescending(p => p.DataEmissao)
            .Take(300)
            .ToListAsync();
    }

    public async Task<PedidoCompra?> ObterPorIdAsync(int id)
    {
        return await _unitOfWork.PedidosCompra.Query()
            .Include(p => p.Fornecedor)
            .Include(p => p.Itens)
            .ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);
    }

    public async Task<Result<PedidoCompra>> CriarAsync(int fornecedorId, string? observacao, IEnumerable<PedidoCompraItemInput> itens)
    {
        if (fornecedorId <= 0)
        {
            return Result.Falha<PedidoCompra>("Selecione um fornecedor valido.");
        }

        var fornecedor = await _unitOfWork.Fornecedores.GetByIdAsync(fornecedorId);
        if (fornecedor == null || !fornecedor.Ativo)
        {
            return Result.Falha<PedidoCompra>("Fornecedor nao encontrado.");
        }

        var itensValidos = itens
            .Where(i => !string.IsNullOrWhiteSpace(i.Descricao) && i.Quantidade > 0 && i.ValorUnitario > 0)
            .ToList();

        if (itensValidos.Count == 0)
        {
            return Result.Falha<PedidoCompra>("Adicione ao menos um item valido ao pedido.");
        }

        var pedido = new PedidoCompra
        {
            Numero = GerarNumeroPedido(),
            FornecedorId = fornecedorId,
            DataEmissao = DateTime.Now,
            Status = "Rascunho",
            Observacao = observacao
        };

        foreach (var item in itensValidos)
        {
            pedido.Itens.Add(new ItemPedidoCompra
            {
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao.Trim(),
                Quantidade = item.Quantidade,
                QuantidadeRecebida = 0,
                ValorUnitario = item.ValorUnitario,
                ValorUnitarioRecebido = null,
                Subtotal = item.Quantidade * item.ValorUnitario
            });
        }

        pedido.Total = pedido.Itens.Sum(i => i.Subtotal);

        await _unitOfWork.PedidosCompra.InsertAsync(pedido);
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok(pedido);
    }

    public async Task<Result<PedidoCompra>> AtualizarAsync(int pedidoId, int fornecedorId, string? observacao, IEnumerable<PedidoCompraItemInput> itens)
    {
        var pedido = await _unitOfWork.PedidosCompra.Query()
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == pedidoId && p.Ativo);

        if (pedido == null)
        {
            return Result.Falha<PedidoCompra>("Pedido nao encontrado.");
        }

        if (!pedido.Status.Equals("Rascunho", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Falha<PedidoCompra>("Somente pedidos em rascunho podem ser editados.");
        }

        if (fornecedorId <= 0)
        {
            return Result.Falha<PedidoCompra>("Selecione um fornecedor valido.");
        }

        var fornecedor = await _unitOfWork.Fornecedores.GetByIdAsync(fornecedorId);
        if (fornecedor == null || !fornecedor.Ativo)
        {
            return Result.Falha<PedidoCompra>("Fornecedor nao encontrado.");
        }

        var itensValidos = itens
            .Where(i => !string.IsNullOrWhiteSpace(i.Descricao) && i.Quantidade > 0 && i.ValorUnitario > 0)
            .ToList();

        if (itensValidos.Count == 0)
        {
            return Result.Falha<PedidoCompra>("Adicione ao menos um item valido ao pedido.");
        }

        var itensAtuais = await _unitOfWork.ItensPedidoCompra.Query()
            .Where(i => i.PedidoCompraId == pedidoId)
            .ToListAsync();

        foreach (var itemAtual in itensAtuais)
        {
            await _unitOfWork.ItensPedidoCompra.DeleteAsync(itemAtual);
        }

        foreach (var item in itensValidos)
        {
            await _unitOfWork.ItensPedidoCompra.InsertAsync(new ItemPedidoCompra
            {
                PedidoCompraId = pedidoId,
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao.Trim(),
                Quantidade = item.Quantidade,
                QuantidadeRecebida = 0,
                ValorUnitario = item.ValorUnitario,
                ValorUnitarioRecebido = null,
                Subtotal = item.Quantidade * item.ValorUnitario
            });
        }

        pedido.FornecedorId = fornecedorId;
        pedido.Observacao = observacao;
        pedido.Total = itensValidos.Sum(i => i.Quantidade * i.ValorUnitario);
        pedido.AtualizadoEm = DateTime.Now;

        await _unitOfWork.PedidosCompra.UpdateAsync(pedido);
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok(pedido);
    }

    public async Task<Result> AtualizarStatusAsync(int pedidoId, string novoStatus)
    {
        var pedido = await _unitOfWork.PedidosCompra.GetByIdAsync(pedidoId);
        if (pedido == null || !pedido.Ativo)
        {
            return Result.Falha("Pedido nao encontrado.");
        }

        if (string.IsNullOrWhiteSpace(novoStatus))
        {
            return Result.Falha("Status invalido.");
        }

        pedido.Status = novoStatus.Trim();
        pedido.AtualizadoEm = DateTime.Now;
        await _unitOfWork.PedidosCompra.UpdateAsync(pedido);
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> ExcluirAsync(int pedidoId)
    {
        var pedido = await _unitOfWork.PedidosCompra.GetByIdAsync(pedidoId);
        if (pedido == null || !pedido.Ativo)
        {
            return Result.Falha("Pedido nao encontrado.");
        }

        pedido.Ativo = false;
        pedido.AtualizadoEm = DateTime.Now;
        await _unitOfWork.PedidosCompra.UpdateAsync(pedido);
        await _unitOfWork.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<RecebimentoPedidoResumo>> ReceberComEntradaEstoqueAsync(int pedidoId)
    {
        var contexto = await CarregarContextoRecebimentoAsync(pedidoId);
        if (!contexto.Sucesso)
        {
            return Result.Falha<RecebimentoPedidoResumo>(contexto.Erro ?? "Falha ao preparar recebimento.");
        }

        var (pedido, usuarioSistema, produtos) = contexto.Valor;
        var itensAtivos = pedido.Itens.Where(i => i.Ativo).ToList();
        var itensComPendencia = itensAtivos
            .Select(i => new { Item = i, Pendente = i.Quantidade - i.QuantidadeRecebida })
            .Where(x => x.Pendente > 0)
            .ToList();

        if (itensComPendencia.Count == 0)
        {
            return Result.Falha<RecebimentoPedidoResumo>("Pedido ja esta totalmente recebido.");
        }

        var itensSemProduto = new List<string>();
        var entradas = new List<EntradaEstoquePlanejada>();

        foreach (var registro in itensComPendencia)
        {
            var produto = LocalizarProdutoParaItem(registro.Item.ProdutoId, registro.Item.Descricao, produtos);
            if (produto == null)
            {
                itensSemProduto.Add(registro.Item.Descricao);
                continue;
            }

            entradas.Add(new EntradaEstoquePlanejada(registro.Item, produto, registro.Pendente, registro.Item.ValorUnitario));
        }

        if (itensSemProduto.Count > 0)
        {
            var detalhes = string.Join(", ", itensSemProduto.Take(3));
            var sufixo = itensSemProduto.Count > 3 ? "..." : string.Empty;
            return Result.Falha<RecebimentoPedidoResumo>(
                $"Nao foi possivel localizar produto para {itensSemProduto.Count} item(ns): {detalhes}{sufixo}. " +
                "Ajuste o vinculo do item com produto.");
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var totalMovimentado = 0m;
            foreach (var entrada in entradas)
            {
                await AplicarEntradaEstoqueAsync(pedido, entrada.Item, entrada.Produto, entrada.QuantidadeReceber, entrada.ValorUnitarioRecebido, usuarioSistema.Id, "Recebimento total do pedido");
                totalMovimentado += entrada.QuantidadeReceber;
            }

            AtualizarStatusPedidoPorRecebimento(pedido);
            await _unitOfWork.PedidosCompra.UpdateAsync(pedido);
            await _unitOfWork.CommitAsync();

            return Result.Ok(new RecebimentoPedidoResumo(
                pedido.Numero,
                entradas.Count,
                totalMovimentado,
                pedido.Fornecedor?.NomeFantasia ?? pedido.Fornecedor?.RazaoSocial ?? "-"));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            return Result.Falha<RecebimentoPedidoResumo>($"Falha no recebimento do pedido: {ex.Message}");
        }
    }

    public async Task<Result<RecebimentoItemResumo>> ReceberItemAsync(int pedidoId, int itemPedidoId, decimal quantidadeRecebida, decimal? valorUnitarioRecebido = null, string? observacao = null)
    {
        if (quantidadeRecebida <= 0)
        {
            return Result.Falha<RecebimentoItemResumo>("Quantidade recebida deve ser maior que zero.");
        }

        var contexto = await CarregarContextoRecebimentoAsync(pedidoId);
        if (!contexto.Sucesso)
        {
            return Result.Falha<RecebimentoItemResumo>(contexto.Erro ?? "Falha ao preparar recebimento.");
        }

        var (pedido, usuarioSistema, produtos) = contexto.Valor;
        var item = pedido.Itens.FirstOrDefault(i => i.Id == itemPedidoId && i.Ativo);
        if (item == null)
        {
            return Result.Falha<RecebimentoItemResumo>("Item do pedido nao encontrado.");
        }

        var pendente = item.Quantidade - item.QuantidadeRecebida;
        if (pendente <= 0)
        {
            return Result.Falha<RecebimentoItemResumo>("Item ja esta totalmente recebido.");
        }

        if (quantidadeRecebida > pendente)
        {
            return Result.Falha<RecebimentoItemResumo>($"Quantidade informada excede pendencia. Pendente atual: {pendente:N3}.");
        }

        var produto = LocalizarProdutoParaItem(item.ProdutoId, item.Descricao, produtos);
        if (produto == null)
        {
            return Result.Falha<RecebimentoItemResumo>("Produto vinculado ao item nao foi localizado.");
        }

        var custoRecebido = valorUnitarioRecebido.HasValue && valorUnitarioRecebido.Value > 0
            ? valorUnitarioRecebido.Value
            : item.ValorUnitario;
        var houveDivergencia = Math.Abs(custoRecebido - item.ValorUnitario) > 0.0001m;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await AplicarEntradaEstoqueAsync(pedido, item, produto, quantidadeRecebida, custoRecebido, usuarioSistema.Id, observacao ?? "Recebimento parcial do pedido");
            AtualizarStatusPedidoPorRecebimento(pedido);
            await _unitOfWork.PedidosCompra.UpdateAsync(pedido);
            await _unitOfWork.CommitAsync();

            var restante = item.Quantidade - item.QuantidadeRecebida;
            return Result.Ok(new RecebimentoItemResumo(
                pedido.Numero,
                item.Descricao,
                quantidadeRecebida,
                item.QuantidadeRecebida,
                restante,
                houveDivergencia,
                custoRecebido));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            return Result.Falha<RecebimentoItemResumo>($"Falha ao receber item: {ex.Message}");
        }
    }

    public async Task<Result<List<RecebimentoHistoricoLinha>>> ListarHistoricoRecebimentoAsync(int pedidoId, int? itemPedidoId = null)
    {
        var pedido = await _unitOfWork.PedidosCompra.Query()
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == pedidoId && p.Ativo);

        if (pedido == null)
        {
            return Result.Falha<List<RecebimentoHistoricoLinha>>("Pedido nao encontrado.");
        }

        ItemPedidoCompra? itemFiltro = null;
        if (itemPedidoId.HasValue)
        {
            itemFiltro = pedido.Itens.FirstOrDefault(i => i.Id == itemPedidoId.Value && i.Ativo);
            if (itemFiltro == null)
            {
                return Result.Falha<List<RecebimentoHistoricoLinha>>("Item do pedido nao encontrado para historico.");
            }
        }

        var movimentosQuery = _unitOfWork.MovimentosEstoque.Query()
            .Include(m => m.Usuario)
            .Include(m => m.Produto)
            .Where(m =>
                m.Ativo &&
                m.Tipo == TipoMovimentoEstoque.Entrada &&
                m.Documento == pedido.Numero);

        if (itemFiltro?.ProdutoId is int produtoId)
        {
            movimentosQuery = movimentosQuery.Where(m => m.ProdutoId == produtoId);
        }

        var movimentos = await movimentosQuery
            .OrderByDescending(m => m.CriadoEm)
            .Take(500)
            .ToListAsync();

        var historico = new List<RecebimentoHistoricoLinha>();
        foreach (var mov in movimentos)
        {
            var itemAssociado = pedido.Itens.FirstOrDefault(i => i.Ativo && i.ProdutoId == mov.ProdutoId)
                               ?? pedido.Itens.FirstOrDefault(i => i.Ativo && mov.Observacao != null && mov.Observacao.Contains(i.Descricao, StringComparison.OrdinalIgnoreCase));

            var valorPlanejado = itemAssociado?.ValorUnitario;
            var valorRecebido = mov.CustoUnitario;
            var divergencia = valorPlanejado.HasValue && valorRecebido.HasValue
                ? Math.Abs(valorPlanejado.Value - valorRecebido.Value) > 0.0001m
                : false;

            historico.Add(new RecebimentoHistoricoLinha(
                mov.CriadoEm,
                mov.Usuario?.Nome ?? "Sistema",
                mov.Produto?.Codigo ?? "-",
                mov.Produto?.Descricao ?? "-",
                mov.Quantidade,
                valorPlanejado,
                valorRecebido,
                divergencia,
                mov.Observacao ?? string.Empty));
        }

        return Result.Ok(historico);
    }

    private async Task<Result<(PedidoCompra Pedido, Usuario Usuario, List<Produto> Produtos)>> CarregarContextoRecebimentoAsync(int pedidoId)
    {
        var pedido = await _unitOfWork.PedidosCompra.Query()
            .Include(p => p.Itens)
            .Include(p => p.Fornecedor)
            .FirstOrDefaultAsync(p => p.Id == pedidoId && p.Ativo);

        if (pedido == null)
        {
            return Result.Falha<(PedidoCompra, Usuario, List<Produto>)>("Pedido nao encontrado.");
        }

        if (pedido.Status.Equals("Cancelado", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Falha<(PedidoCompra, Usuario, List<Produto>)>("Pedido cancelado nao pode ser recebido.");
        }

        var itensAtivos = pedido.Itens.Where(i => i.Ativo).ToList();
        if (itensAtivos.Count == 0)
        {
            return Result.Falha<(PedidoCompra, Usuario, List<Produto>)>("Pedido sem itens ativos para recebimento.");
        }

        var usuarioSistema = await _unitOfWork.Usuarios.Query()
            .Where(u => u.Ativo)
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync();

        if (usuarioSistema == null)
        {
            return Result.Falha<(PedidoCompra, Usuario, List<Produto>)>("Cadastre ao menos um usuario ativo para registrar movimentos de estoque.");
        }

        var produtos = await _unitOfWork.Produtos.Query()
            .Where(p => p.Ativo && p.ControlaEstoque)
            .ToListAsync();

        if (produtos.Count == 0)
        {
            return Result.Falha<(PedidoCompra, Usuario, List<Produto>)>("Nenhum produto ativo com controle de estoque foi encontrado.");
        }

        return Result.Ok((pedido, usuarioSistema, produtos));
    }

    private async Task AplicarEntradaEstoqueAsync(
        PedidoCompra pedido,
        ItemPedidoCompra item,
        Produto produto,
        decimal quantidadeReceber,
        decimal custoUnitarioRecebido,
        int usuarioId,
        string observacao)
    {
        var saldoAnterior = produto.Estoque;
        var saldoAtual = saldoAnterior + quantidadeReceber;

        produto.Estoque = saldoAtual;
        produto.PrecoCusto = custoUnitarioRecebido > 0 ? custoUnitarioRecebido : produto.PrecoCusto;
        produto.AtualizadoEm = DateTime.Now;
        await _unitOfWork.Produtos.UpdateAsync(produto);

        item.QuantidadeRecebida += quantidadeReceber;
        item.ValorUnitarioRecebido = custoUnitarioRecebido;
        item.AtualizadoEm = DateTime.Now;
        await _unitOfWork.ItensPedidoCompra.UpdateAsync(item);

        var movimento = new MovimentoEstoque
        {
            ProdutoId = produto.Id,
            Tipo = TipoMovimentoEstoque.Entrada,
            Quantidade = quantidadeReceber,
            SaldoAnterior = saldoAnterior,
            SaldoAtual = saldoAtual,
            CustoUnitario = custoUnitarioRecebido,
            Documento = pedido.Numero,
            FornecedorId = pedido.FornecedorId,
            UsuarioId = usuarioId,
            Observacao = $"{observacao} - {item.Descricao}"
        };

        await _unitOfWork.MovimentosEstoque.InsertAsync(movimento);
    }

    private static void AtualizarStatusPedidoPorRecebimento(PedidoCompra pedido)
    {
        var itensAtivos = pedido.Itens.Where(i => i.Ativo).ToList();
        var totalPendente = itensAtivos.Sum(i => Math.Max(0m, i.Quantidade - i.QuantidadeRecebida));
        pedido.Status = totalPendente <= 0 ? "Recebido" : "Parcial";
        pedido.AtualizadoEm = DateTime.Now;
    }

    private static Produto? LocalizarProdutoParaItem(int? produtoId, string descricaoItem, IReadOnlyCollection<Produto> produtos)
    {
        if (produtoId.HasValue)
        {
            var porId = produtos.FirstOrDefault(p => p.Id == produtoId.Value);
            if (porId != null)
            {
                return porId;
            }
        }

        var descricaoNormalizada = Normalizar(descricaoItem);
        if (string.IsNullOrWhiteSpace(descricaoNormalizada))
        {
            return null;
        }

        var token = ExtrairTokenCodigo(descricaoItem);
        if (!string.IsNullOrWhiteSpace(token))
        {
            var porCodigo = produtos.FirstOrDefault(p =>
                string.Equals(Normalizar(p.Codigo), token, StringComparison.Ordinal) ||
                string.Equals(Normalizar(p.CodigoBarras ?? string.Empty), token, StringComparison.Ordinal));

            if (porCodigo != null)
            {
                return porCodigo;
            }
        }

        return produtos.FirstOrDefault(p =>
            string.Equals(Normalizar(p.Descricao), descricaoNormalizada, StringComparison.Ordinal));
    }

    private static string ExtrairTokenCodigo(string descricao)
    {
        if (string.IsNullOrWhiteSpace(descricao))
        {
            return string.Empty;
        }

        var separadores = new[] { ' ', '-', '/', '\\', '|', ';', ':' };
        var token = descricao.Split(separadores, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return Normalizar(token ?? string.Empty);
    }

    private static string Normalizar(string texto)
    {
        return texto.Trim().ToUpperInvariant();
    }

    private static string GerarNumeroPedido()
    {
        return $"PC-{DateTime.Now:yyyyMMdd-HHmmss}";
    }

    private sealed record EntradaEstoquePlanejada(ItemPedidoCompra Item, Produto Produto, decimal QuantidadeReceber, decimal ValorUnitarioRecebido);
}

public sealed record PedidoCompraItemInput(string Descricao, decimal Quantidade, decimal ValorUnitario, int? ProdutoId = null);
public sealed record RecebimentoPedidoResumo(string NumeroPedido, int QuantidadeItens, decimal QuantidadeTotalMovimentada, string Fornecedor);
public sealed record RecebimentoItemResumo(
    string NumeroPedido,
    string ItemDescricao,
    decimal QuantidadeRecebidaAgora,
    decimal QuantidadeRecebidaAcumulada,
    decimal QuantidadePendente,
    bool HouveDivergenciaValor,
    decimal ValorUnitarioRecebido);
public sealed record RecebimentoHistoricoLinha(
    DateTime DataHora,
    string Usuario,
    string ProdutoCodigo,
    string ProdutoDescricao,
    decimal Quantidade,
    decimal? ValorPlanejado,
    decimal? ValorRecebido,
    bool Divergencia,
    string Observacao);
