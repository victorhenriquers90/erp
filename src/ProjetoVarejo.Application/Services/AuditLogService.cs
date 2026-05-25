using Microsoft.EntityFrameworkCore;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;
using ProjetoVarejo.Infrastructure.Data;

namespace ProjetoVarejo.Application.Services;

public class AuditLogService
{
    private readonly AppDbContext _db;
    private readonly SessaoApp _sessao;

    public AuditLogService(AppDbContext db, SessaoApp sessao)
    {
        _db = db; _sessao = sessao;
    }

    public async Task RegistrarAsync(string entidade, string registroId, TipoAuditoria tipo, string? antes = null, string? depois = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UsuarioId = _sessao.UsuarioLogado?.Id,
            Entidade = entidade,
            RegistroId = registroId,
            Tipo = tipo,
            ValoresAntes = antes,
            ValoresDepois = depois
        });
        await _db.SaveChangesAsync();
    }

    public Task<List<AuditLog>> ListarAsync(DateTime? de = null, DateTime? ate = null, string? entidade = null, int? usuarioId = null)
    {
        var q = _db.AuditLogs.Include(x => x.Usuario).AsQueryable();
        if (de.HasValue) q = q.Where(x => x.Data >= de.Value);
        if (ate.HasValue) q = q.Where(x => x.Data <= ate.Value);
        if (!string.IsNullOrWhiteSpace(entidade)) q = q.Where(x => x.Entidade == entidade);
        if (usuarioId.HasValue) q = q.Where(x => x.UsuarioId == usuarioId);
        return q.OrderByDescending(x => x.Data).Take(1000).ToListAsync();
    }
}
