using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Api.Extensions;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using Serilog;
using Microsoft.AspNetCore.Mvc;

namespace ProjetoVarejo.Api.Endpoints;

/// <summary>
/// API endpoints for supplier (fornecedor) management.
/// </summary>
public static class FornecedoresEndpoints
{
    public static void MapFornecedoresEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/fornecedores")
            .WithName("Fornecedores")
            .WithOpenApi()
            .WithTags("Fornecedores")
            .RequireAuthorization();

        group.MapGet("/", ListarFornecedores).WithName("ListarFornecedores");
        group.MapGet("/{id}", ObterFornecedor).WithName("ObterFornecedor");
        group.MapPost("/", CriarFornecedor)
            .WithName("CriarFornecedor")
            .RequireAuthorization("AdminOrGerente");
        group.MapPut("/{id}", AtualizarFornecedor)
            .WithName("AtualizarFornecedor")
            .RequireAuthorization("AdminOrGerente");
        group.MapDelete("/{id}", DeletarFornecedor)
            .WithName("DeletarFornecedor")
            .RequireAuthorization("AdminOnly");
        group.MapGet("/{id}/produtos", ListarProdutosFornecedor).WithName("ListarProdutosFornecedor");
    }

    private static async Task<IResult> ListarFornecedores(
        [FromServices] FornecedorService svc,
        [FromQuery] string? filtro = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            Log.Information("Listando fornecedores. filtro={Filtro}", filtro);
            var lista = await svc.ListarAsync(filtro);
            var paginado = lista.Paginate(page, pageSize);
            return Results.Ok(new ApiResponse<PagedResult<Fornecedor>>
            {
                Success = true,
                Data = paginado,
                Message = $"{paginado.TotalCount} fornecedor(es) encontrado(s)"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao listar fornecedores");
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> ObterFornecedor(int id, [FromServices] FornecedorService svc)
    {
        try
        {
            Log.Information("Obtendo fornecedor {Id}", id);
            var fornecedor = await svc.BuscarPorIdAsync(id);
            if (fornecedor == null)
                return Results.NotFound(ApiResponse.Error($"Fornecedor {id} não encontrado", 404));
            return Results.Ok(new ApiResponse<Fornecedor> { Success = true, Data = fornecedor });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter fornecedor {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> CriarFornecedor(
        [FromBody] FornecedorRequest req,
        [FromServices] FornecedorService svc)
    {
        try
        {
            Log.Information("Criando fornecedor: {Nome}", req.RazaoSocial);
            var fornecedor = new Fornecedor
            {
                RazaoSocial = req.RazaoSocial,
                NomeFantasia = req.NomeFantasia,
                Cnpj = req.Cnpj,
                Email = req.Email,
                Telefone = req.Telefone,
                Contato = req.Contato,
                Observacao = req.Observacao
            };
            var resultado = await svc.SalvarAsync(fornecedor);
            return resultado.Sucesso
                ? Results.Created($"/api/fornecedores/{resultado.Valor!.Id}",
                    new ApiResponse<Fornecedor> { Success = true, Data = resultado.Valor, Message = "Fornecedor criado" })
                : Results.BadRequest(ApiResponse.Error(resultado.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao criar fornecedor");
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> AtualizarFornecedor(
        int id,
        [FromBody] FornecedorRequest req,
        [FromServices] FornecedorService svc)
    {
        try
        {
            Log.Information("Atualizando fornecedor {Id}", id);
            var fornecedor = await svc.BuscarPorIdAsync(id);
            if (fornecedor == null)
                return Results.NotFound(ApiResponse.Error($"Fornecedor {id} não encontrado", 404));

            fornecedor.RazaoSocial = req.RazaoSocial;
            fornecedor.NomeFantasia = req.NomeFantasia;
            fornecedor.Cnpj = req.Cnpj;
            fornecedor.Email = req.Email;
            fornecedor.Telefone = req.Telefone;
            fornecedor.Contato = req.Contato;
            fornecedor.Observacao = req.Observacao;

            var resultado = await svc.SalvarAsync(fornecedor);
            return resultado.Sucesso
                ? Results.Ok(new ApiResponse<Fornecedor> { Success = true, Data = resultado.Valor, Message = "Fornecedor atualizado" })
                : Results.BadRequest(ApiResponse.Error(resultado.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao atualizar fornecedor {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> DeletarFornecedor(int id, [FromServices] FornecedorService svc)
    {
        try
        {
            Log.Information("Inativando fornecedor {Id}", id);
            var resultado = await svc.ExcluirAsync(id);
            return resultado.Sucesso
                ? Results.Ok(ApiResponse.Ok("Fornecedor inativado"))
                : Results.NotFound(ApiResponse.Error(resultado.Erro ?? "Operação falhou", 404));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao inativar fornecedor {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> ListarProdutosFornecedor(
        int id,
        [FromServices] FornecedorService svc,
        [FromServices] ProjetoVarejo.Application.Contracts.Repositories.IUnitOfWork uow,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            Log.Information("Listando produtos do fornecedor {Id}", id);
            var fornecedor = await svc.BuscarPorIdAsync(id);
            if (fornecedor == null)
                return Results.NotFound(ApiResponse.Error($"Fornecedor {id} não encontrado", 404));

            // Produtos associados ao fornecedor via movimentos de estoque (entradas)
            var produtoIds = await uow.MovimentosEstoque.Query()
                .Where(m => m.FornecedorId == id)
                .Select(m => m.ProdutoId)
                .Distinct()
                .ToListAsync();

            var produtos = await uow.Produtos.Query()
                .Where(p => produtoIds.Contains(p.Id) && p.Ativo)
                .OrderBy(p => p.Descricao)
                .ToListAsync();
            var paginado = produtos.Paginate(page, pageSize);
            return Results.Ok(new ApiResponse<PagedResult<ProjetoVarejo.Domain.Entities.Produto>>
            {
                Success = true,
                Data = paginado,
                Message = $"{paginado.TotalCount} produto(s)"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao listar produtos do fornecedor {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }
}

public class FornecedorRequest
{
    public string RazaoSocial { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Contato { get; set; }
    public string? Observacao { get; set; }
}

// Kept for backward compatibility
public class CreateFornecedorRequest { public string Nome { get; set; } = string.Empty; }
public class UpdateFornecedorRequest { public string Nome { get; set; } = string.Empty; }
