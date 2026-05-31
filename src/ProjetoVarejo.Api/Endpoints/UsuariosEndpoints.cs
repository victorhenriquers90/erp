using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Api.Extensions;
using ProjetoVarejo.Api.Models;
using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Services;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Shared;
using Serilog;
using Microsoft.AspNetCore.Mvc;

namespace ProjetoVarejo.Api.Endpoints;

/// <summary>
/// API endpoints for user management.
/// Provides CRUD operations for system users and their permissions.
/// </summary>
public static class UsuariosEndpoints
{
    public static void MapUsuariosEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/usuarios")
            .WithName("Usuarios")
            .WithOpenApi()
            .WithTags("Usuarios")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", ListarUsuarios).WithName("ListarUsuarios");
        group.MapGet("/{id}", ObterUsuario).WithName("ObterUsuario");
        group.MapPost("/", CriarUsuario).WithName("CriarUsuario");
        group.MapPut("/{id}", AtualizarUsuario).WithName("AtualizarUsuario");
        group.MapPost("/{id}/redefinir-senha", RedefinirSenha).WithName("RedefinirSenha");
        group.MapPost("/{id}/alternar-ativo", AlternarAtivo).WithName("AlternarAtivo");

        // Permissions
        group.MapGet("/{id}/permissoes", ListarPermissoes).WithName("ListarPermissoesUsuario");
        group.MapPost("/{id}/permissoes", ConcederPermissao).WithName("ConcederPermissao");
        group.MapDelete("/{id}/permissoes/{permissao}", RevogarPermissao).WithName("RevogarPermissao");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Users
    // ──────────────────────────────────────────────────────────────────────────

    private static async Task<IResult> ListarUsuarios(
        [FromServices] UsuarioService svc,
        [FromQuery] string? filtro = null,
        [FromQuery] bool incluirInativos = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            Log.Information("Listando usuários. filtro={Filtro}", filtro);
            var lista = await svc.ListarAsync(filtro, incluirInativos);
            // Never expose password hashes
            foreach (var u in lista) u.SenhaHash = string.Empty;
            var paginado = lista.Paginate(page, pageSize);
            return Results.Ok(new ApiResponse<PagedResult<Usuario>>
            {
                Success = true,
                Data = paginado,
                Message = $"{paginado.TotalCount} usuário(s)"
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao listar usuários");
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> ObterUsuario(int id, [FromServices] UsuarioService svc)
    {
        try
        {
            Log.Information("Obtendo usuário {Id}", id);
            var usuario = await svc.BuscarPorIdAsync(id);
            if (usuario == null)
                return Results.NotFound(ApiResponse.Error($"Usuário {id} não encontrado", 404));
            usuario.SenhaHash = string.Empty;
            return Results.Ok(new ApiResponse<Usuario> { Success = true, Data = usuario });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao obter usuário {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> CriarUsuario(
        [FromBody] UsuarioRequest req,
        [FromServices] UsuarioService svc)
    {
        try
        {
            Log.Information("Criando usuário: {Login}", req.Login);
            var usuario = new Usuario
            {
                Login = req.Login,
                Nome = req.Nome,
                Perfil = req.Perfil,
                Ativo = true
            };
            var res = await svc.SalvarAsync(usuario, req.Senha);
            if (!res.Sucesso)
                return Results.BadRequest(ApiResponse.Error(res.Erro ?? "Operação falhou"));

            res.Valor!.SenhaHash = string.Empty;
            return Results.Created($"/api/usuarios/{res.Valor.Id}",
                new ApiResponse<Usuario> { Success = true, Data = res.Valor, Message = "Usuário criado" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao criar usuário");
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> AtualizarUsuario(
        int id,
        [FromBody] UsuarioUpdateRequest req,
        [FromServices] UsuarioService svc)
    {
        try
        {
            Log.Information("Atualizando usuário {Id}", id);
            var usuario = await svc.BuscarPorIdAsync(id);
            if (usuario == null)
                return Results.NotFound(ApiResponse.Error($"Usuário {id} não encontrado", 404));

            usuario.Nome = req.Nome;
            usuario.Perfil = req.Perfil;
            usuario.Ativo = req.Ativo;

            var res = await svc.SalvarAsync(usuario);
            if (!res.Sucesso)
                return Results.BadRequest(ApiResponse.Error(res.Erro ?? "Operação falhou"));

            res.Valor!.SenhaHash = string.Empty;
            return Results.Ok(new ApiResponse<Usuario> { Success = true, Data = res.Valor, Message = "Usuário atualizado" });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao atualizar usuário {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> RedefinirSenha(
        int id,
        [FromBody] RedefinirSenhaRequest req,
        [FromServices] UsuarioService svc)
    {
        try
        {
            Log.Information("Redefinindo senha do usuário {Id}", id);
            var res = await svc.RedefinirSenhaAsync(id, req.NovaSenha);
            return res.Sucesso
                ? Results.Ok(ApiResponse.Ok("Senha redefinida com sucesso"))
                : Results.BadRequest(ApiResponse.Error(res.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao redefinir senha do usuário {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> AlternarAtivo(int id, [FromServices] UsuarioService svc)
    {
        try
        {
            Log.Information("Alternando status do usuário {Id}", id);
            var res = await svc.AlternarAtivoAsync(id);
            return res.Sucesso
                ? Results.Ok(ApiResponse.Ok("Status do usuário atualizado"))
                : Results.BadRequest(ApiResponse.Error(res.Erro ?? "Operação falhou"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao alternar status do usuário {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Permissions
    // ──────────────────────────────────────────────────────────────────────────

    private static async Task<IResult> ListarPermissoes(int id, [FromServices] IUnitOfWork uow)
    {
        try
        {
            Log.Information("Listando permissões do usuário {Id}", id);
            var usuario = await uow.Usuarios.GetByIdAsync(id);
            if (usuario == null)
                return Results.NotFound(ApiResponse.Error($"Usuário {id} não encontrado", 404));

            var perfilPadrao = PermissaoService.PermissoesPadrao.TryGetValue(usuario.Perfil, out var pp)
                ? pp.Select(p => p.ToString()).OrderBy(p => p).ToList()
                : new List<string>();

            var custom = await uow.UsuarioPermissoes.Query()
                .Where(p => p.UsuarioId == id)
                .Select(p => p.Permissao.ToString())
                .OrderBy(p => p)
                .ToListAsync();

            return Results.Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { PermissoesPerfil = perfilPadrao, PermissoesCustomizadas = custom }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao listar permissões do usuário {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> ConcederPermissao(
        int id,
        [FromBody] PermissaoRequest req,
        [FromServices] IUnitOfWork uow)
    {
        try
        {
            Log.Information("Concedendo permissão {Permissao} ao usuário {Id}", req.Permissao, id);
            var usuario = await uow.Usuarios.GetByIdAsync(id);
            if (usuario == null)
                return Results.NotFound(ApiResponse.Error($"Usuário {id} não encontrado", 404));

            var existe = await uow.UsuarioPermissoes.Query()
                .AnyAsync(p => p.UsuarioId == id && p.Permissao == req.Permissao);
            if (!existe)
            {
                await uow.UsuarioPermissoes.InsertAsync(new UsuarioPermissao
                {
                    UsuarioId = id,
                    Permissao = req.Permissao
                });
                await uow.SaveChangesAsync();
            }
            return Results.Ok(ApiResponse.Ok("Permissão concedida"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao conceder permissão ao usuário {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }

    private static async Task<IResult> RevogarPermissao(
        int id,
        Permissao permissao,
        [FromServices] IUnitOfWork uow)
    {
        try
        {
            Log.Information("Revogando permissão {Permissao} do usuário {Id}", permissao, id);
            var p = await uow.UsuarioPermissoes.Query()
                .FirstOrDefaultAsync(x => x.UsuarioId == id && x.Permissao == permissao);

            if (p != null)
            {
                await uow.UsuarioPermissoes.DeleteAsync(p);
                await uow.SaveChangesAsync();
            }
            return Results.Ok(ApiResponse.Ok("Permissão revogada"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erro ao revogar permissão do usuário {Id}", id);
            return Results.BadRequest(ApiResponse.Error("Erro: " + ex.Message));
        }
    }
}

// ──────────────────────────────────────────────────────────────────────────────
// Request models
// ──────────────────────────────────────────────────────────────────────────────

public class UsuarioRequest
{
    public string Login { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Caixa;
}

public class UsuarioUpdateRequest
{
    public string Nome { get; set; } = string.Empty;
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Caixa;
    public bool Ativo { get; set; } = true;
}

public class RedefinirSenhaRequest
{
    public string NovaSenha { get; set; } = string.Empty;
}

public class PermissaoRequest
{
    public Permissao Permissao { get; set; }
}
