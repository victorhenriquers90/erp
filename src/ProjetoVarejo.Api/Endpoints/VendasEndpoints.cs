using ProjetoVarejo.Api.Extensions;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Application.Contracts.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Serilog;
using Microsoft.AspNetCore.Mvc;

namespace ProjetoVarejo.Api.Endpoints;

/// <summary>
/// API endpoints for sales management.
/// Provides comprehensive CRUD operations for sales (vendas), items, and payments.
/// </summary>
public static class VendasEndpoints
{
    private const string GroupName = "Vendas";
    private const string GroupDescription = "Gerenciamento de vendas - criar, listar, finalizar, cancelar";

    /// <summary>
    /// Maps all sales endpoints to the application.
    /// </summary>
    public static void MapVendasEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/vendas")
            .WithName(GroupName)
            .WithOpenApi()
            .WithTags(GroupName)
            .WithDescription(GroupDescription)
            .RequireAuthorization();

        // GET endpoints
        group.MapGet("/", ListarVendas)
            .WithName("ListarVendas")
            .WithDescription("Lista todas as vendas com filtros opcionais e paginação")
            .WithSummary("Listar vendas");

        group.MapGet("/{id}", ObterVenda)
            .WithName("ObterVenda")
            .WithDescription("Obtém os detalhes de uma venda específica incluindo itens e pagamentos")
            .WithSummary("Obter venda");

        // POST endpoints
        group.MapPost("/", CriarVenda)
            .WithName("CriarVenda")
            .WithDescription("Cria uma nova venda")
            .WithSummary("Criar venda");

        group.MapPost("/{id}/items", AdicionarItem)
            .WithName("AdicionarItem")
            .WithDescription("Adiciona um item a uma venda")
            .WithSummary("Adicionar item");

        group.MapPost("/{id}/desconto", AplicarDesconto)
            .WithName("AplicarDesconto")
            .WithDescription("Aplica um desconto a uma venda")
            .WithSummary("Aplicar desconto")
            .RequireAuthorization("CanApplyDiscount");

        group.MapPost("/{id}/finalize", FinalizarVenda)
            .WithName("FinalizarVenda")
            .WithDescription("Finaliza uma venda com pagamentos")
            .WithSummary("Finalizar venda")
            .RequireAuthorization("AdminOrGerente");

        group.MapPost("/{id}/cancel", CancelarVenda)
            .WithName("CancelarVenda")
            .WithDescription("Cancela uma venda")
            .WithSummary("Cancelar venda")
            .RequireAuthorization("CanCancelSale");

        // DELETE endpoints
        group.MapDelete("/{id}/items/{itemId}", RemoverItem)
            .WithName("RemoverItem")
            .WithDescription("Remove um item de uma venda")
            .WithSummary("Remover item");
    }

    /// <summary>
    /// GET /api/vendas - List all sales with optional filters and pagination
    /// </summary>
    private static async Task<IResult> ListarVendas(
        [FromServices] IVendaService vendaService,
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] StatusVenda? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            Log.Information("Listando vendas com filtros: dataInicio={DataInicio}, dataFim={DataFim}, status={Status}, page={Page}, pageSize={PageSize}",
                dataInicio, dataFim, status, page, pageSize);

            var vendas = await vendaService.ListarAsync(dataInicio, dataFim, status);
            var paginado = vendas.Paginate(page, pageSize);

            return Results.Ok(new ApiResponse<PagedResult<Venda>>
            {
                Success = true,
                Data = paginado,
                Message = $"Total de {paginado.TotalCount} vendas encontradas"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao listar vendas");
            return Results.BadRequest(ApiResponse.Error(
                "Erro ao listar vendas: " + ex.Message, 400));
        }
    }

    /// <summary>
    /// GET /api/vendas/{id} - Get sale details by ID
    /// </summary>
    private static async Task<IResult> ObterVenda(
        int id,
        [FromServices] IVendaService vendaService)
    {
        try
        {
            Log.Information("Obtendo venda {VendaId}", id);

            var venda = await vendaService.BuscarAsync(id);
            if (venda == null)
            {
                return Results.NotFound(ApiResponse.Error(
                    $"Venda {id} não encontrada", 404));
            }

            return Results.Ok(new ApiResponse<Venda>
            {
                Success = true,
                Data = venda
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter venda {VendaId}", id);
            return Results.BadRequest(ApiResponse.Error(
                "Erro ao obter venda: " + ex.Message, 400));
        }
    }

    /// <summary>
    /// POST /api/vendas - Create a new sale
    /// </summary>
    private static async Task<IResult> CriarVenda(
        [FromServices] IVendaService vendaService)
    {
        try
        {
            Log.Information("Criando nova venda");

            var resultado = await vendaService.NovaVendaAsync();
            return resultado.ToApiResponse();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao criar venda");
            return Results.BadRequest(ApiResponse.Error(
                "Erro ao criar venda: " + ex.Message, 400));
        }
    }

    /// <summary>
    /// POST /api/vendas/{id}/items - Add an item to a sale
    /// </summary>
    private static async Task<IResult> AdicionarItem(
        int id,
        AdicionarItemRequest request,
        [FromServices] IVendaService vendaService)
    {
        try
        {
            Log.Information("Adicionando item à venda {VendaId}: produtoId={ProdutoId}, quantidade={Quantidade}",
                id, request.ProdutoId, request.Quantidade);

            if (request.Quantidade <= 0)
            {
                return Results.BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Quantidade deve ser maior que zero",
                    ErrorCode = 400
                });
            }

            var resultado = await vendaService.AdicionarItemAsync(
                id, request.ProdutoId, request.Quantidade, request.PrecoOverride);

            return resultado.ToApiResponse();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao adicionar item à venda {VendaId}", id);
            return Results.BadRequest(ApiResponse.Error(
                "Erro ao adicionar item: " + ex.Message, 400));
        }
    }

    /// <summary>
    /// DELETE /api/vendas/{id}/items/{itemId} - Remove an item from a sale
    /// </summary>
    private static async Task<IResult> RemoverItem(
        int id,
        int itemId,
        [FromServices] IVendaService vendaService)
    {
        try
        {
            Log.Information("Removendo item {ItemId} da venda {VendaId}", itemId, id);

            var resultado = await vendaService.RemoverItemAsync(itemId);
            return resultado.ToApiResponse();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao remover item {ItemId} da venda {VendaId}", itemId, id);
            return Results.BadRequest(ApiResponse.Error(
                "Erro ao remover item: " + ex.Message, 400));
        }
    }

    /// <summary>
    /// POST /api/vendas/{id}/desconto - Apply a discount to a sale
    /// </summary>
    private static async Task<IResult> AplicarDesconto(
        int id,
        AplicarDescontoRequest request,
        [FromServices] IVendaService vendaService)
    {
        try
        {
            Log.Information("Aplicando desconto à venda {VendaId}: desconto={Desconto}", id, request.Desconto);

            if (request.Desconto < 0)
            {
                return Results.BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Desconto não pode ser negativo",
                    ErrorCode = 400
                });
            }

            var resultado = await vendaService.AplicarDescontoAsync(id, request.Desconto);
            return resultado.ToApiResponse();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao aplicar desconto à venda {VendaId}", id);
            return Results.BadRequest(ApiResponse.Error(
                "Erro ao aplicar desconto: " + ex.Message, 400));
        }
    }

    /// <summary>
    /// POST /api/vendas/{id}/finalize - Finalize a sale with payments
    /// </summary>
    private static async Task<IResult> FinalizarVenda(
        int id,
        FinalizarVendaRequest request,
        [FromServices] IVendaService vendaService)
    {
        try
        {
            Log.Information("Finalizando venda {VendaId}", id);

            if (request.Pagamentos == null || !request.Pagamentos.Any())
            {
                return Results.BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Pelo menos um pagamento é obrigatório",
                    ErrorCode = 400
                });
            }

            var resultado = await vendaService.FinalizarAsync(id, request.Pagamentos, request.ClienteId);
            return resultado.ToApiResponse();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao finalizar venda {VendaId}", id);
            return Results.BadRequest(ApiResponse.Error(
                "Erro ao finalizar venda: " + ex.Message, 400));
        }
    }

    /// <summary>
    /// POST /api/vendas/{id}/cancel - Cancel a sale
    /// </summary>
    private static async Task<IResult> CancelarVenda(
        int id,
        CancelarVendaRequest request,
        [FromServices] IVendaService vendaService)
    {
        try
        {
            Log.Information("Cancelando venda {VendaId}: motivo={Motivo}", id, request.Motivo);

            if (string.IsNullOrWhiteSpace(request.Motivo))
            {
                return Results.BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Motivo do cancelamento é obrigatório",
                    ErrorCode = 400
                });
            }

            var resultado = await vendaService.CancelarAsync(id, request.Motivo);
            return resultado.ToApiResponse();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao cancelar venda {VendaId}", id);
            return Results.BadRequest(ApiResponse.Error(
                "Erro ao cancelar venda: " + ex.Message, 400));
        }
    }
}

/// <summary>Request model for adding an item to a sale.</summary>
public class AdicionarItemRequest
{
    /// <summary>Gets or sets the product ID.</summary>
    public int ProdutoId { get; set; }

    /// <summary>Gets or sets the quantity of the item.</summary>
    public decimal Quantidade { get; set; }

    /// <summary>Gets or sets the optional price override (if different from product's sale price).</summary>
    public decimal? PrecoOverride { get; set; }
}

/// <summary>Request model for applying a discount to a sale.</summary>
public class AplicarDescontoRequest
{
    /// <summary>Gets or sets the discount amount.</summary>
    public decimal Desconto { get; set; }
}

/// <summary>Request model for finalizing a sale.</summary>
public class FinalizarVendaRequest
{
    /// <summary>Gets or sets the list of payments.</summary>
    public List<PagamentoVenda> Pagamentos { get; set; } = new();

    /// <summary>Gets or sets the optional customer ID.</summary>
    public int? ClienteId { get; set; }
}

/// <summary>Request model for canceling a sale.</summary>
public class CancelarVendaRequest
{
    /// <summary>Gets or sets the cancellation reason.</summary>
    public string Motivo { get; set; } = string.Empty;
}
