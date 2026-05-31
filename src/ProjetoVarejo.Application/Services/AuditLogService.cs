using ProjetoVarejo.Application.Contracts.Repositories;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ProjetoVarejo.Application.Services;

public class AuditLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly SessaoApp _sessao;

    public AuditLogService(IUnitOfWork unitOfWork, SessaoApp sessao)
    {
        _unitOfWork = unitOfWork; _sessao = sessao;
    }

    public async Task RegistrarAsync(string entidade, string registroId, TipoAuditoria tipo, string? antes = null, string? depois = null)
    {
        await _unitOfWork.AuditLogs.InsertAsync(new AuditLog
        {
            UsuarioId = _sessao.UsuarioLogado?.Id,
            Entidade = entidade,
            RegistroId = registroId,
            Tipo = tipo,
            ValoresAntes = antes,
            ValoresDepois = depois
        });
        await _unitOfWork.SaveChangesAsync();
    }

    public Task<List<AuditLog>> ListarAsync(DateTime? de = null, DateTime? ate = null, string? entidade = null, int? usuarioId = null)
    {
        var q = _unitOfWork.AuditLogs.Query().Include(x => x.Usuario).AsQueryable();
        if (de.HasValue) q = q.Where(x => x.Data >= de.Value);
        if (ate.HasValue) q = q.Where(x => x.Data <= ate.Value);
        if (!string.IsNullOrWhiteSpace(entidade)) q = q.Where(x => x.Entidade == entidade);
        if (usuarioId.HasValue) q = q.Where(x => x.UsuarioId == usuarioId);
        return q.OrderByDescending(x => x.Data).Take(1000).ToListAsync();
    }
}
