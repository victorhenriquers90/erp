using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ProjetoVarejo.Application.Sessao;
using ProjetoVarejo.Domain.Entities;

namespace ProjetoVarejo.Application.Auditing;

/// <summary>
/// Intercepta SaveChanges para registrar AuditLog automaticamente para entidades sensíveis.
/// </summary>
public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<string> EntidadesAuditadas = new(StringComparer.Ordinal)
    {
        nameof(Venda),
        nameof(NotaFiscal),
        nameof(ContaFinanceira),
        nameof(Produto),
        nameof(Cliente),
        nameof(Fornecedor),
        nameof(MovimentoCaixa),
        nameof(CaixaSessao),
        nameof(Usuario),
        nameof(EmpresaConfig),
        nameof(UsuarioPermissao)
    };

    private static readonly HashSet<string> PropriedadesSensiveis = new(StringComparer.OrdinalIgnoreCase)
    {
        "SenhaHash", "CertificadoSenha", "CscToken"
    };

    private readonly SessaoApp _sessao;

    public AuditSaveChangesInterceptor(SessaoApp sessao)
    {
        _sessao = sessao;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var ctx = eventData.Context;
        if (ctx == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var logs = ColetarLogs(ctx);
        foreach (var log in logs) ctx.Set<AuditLog>().Add(log);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        var ctx = eventData.Context;
        if (ctx == null) return base.SavingChanges(eventData, result);

        var logs = ColetarLogs(ctx);
        foreach (var log in logs) ctx.Set<AuditLog>().Add(log);

        return base.SavingChanges(eventData, result);
    }

    private List<AuditLog> ColetarLogs(DbContext ctx)
    {
        var logs = new List<AuditLog>();
        var entries = ctx.ChangeTracker.Entries().ToList(); // snapshot — não enumerar enquanto mutamos

        foreach (var entry in entries)
        {
            var nome = entry.Entity.GetType().Name;
            if (!EntidadesAuditadas.Contains(nome)) continue;

            TipoAuditoria? tipo = entry.State switch
            {
                EntityState.Added => TipoAuditoria.Insert,
                EntityState.Modified => TipoAuditoria.Update,
                EntityState.Deleted => TipoAuditoria.Delete,
                _ => null
            };
            if (tipo == null) continue;

            var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            var registroId = idProp?.CurrentValue?.ToString() ?? "?";

            string? antes = null, depois = null;
            try
            {
                if (tipo == TipoAuditoria.Update)
                {
                    var diff = ColetarDiff(entry);
                    if (diff.antes.Count == 0) continue; // só timestamp mudou — não logar
                    antes = JsonSerializer.Serialize(diff.antes);
                    depois = JsonSerializer.Serialize(diff.depois);
                }
                else if (tipo == TipoAuditoria.Insert)
                {
                    depois = JsonSerializer.Serialize(Snapshot(entry, useOriginal: false));
                }
                else if (tipo == TipoAuditoria.Delete)
                {
                    antes = JsonSerializer.Serialize(Snapshot(entry, useOriginal: true));
                }
            }
            catch { /* não bloquear save por falha de auditoria */ }

            logs.Add(new AuditLog
            {
                UsuarioId = _sessao.UsuarioLogado?.Id,
                Entidade = nome,
                RegistroId = registroId,
                Tipo = tipo.Value,
                ValoresAntes = antes,
                ValoresDepois = depois
            });
        }
        return logs;
    }

    private static (Dictionary<string, object?> antes, Dictionary<string, object?> depois) ColetarDiff(EntityEntry entry)
    {
        var antes = new Dictionary<string, object?>();
        var depois = new Dictionary<string, object?>();
        foreach (var p in entry.Properties)
        {
            if (!p.IsModified) continue;
            var nome = p.Metadata.Name;
            if (nome is "CriadoEm" or "AtualizadoEm") continue;
            antes[nome] = PropriedadesSensiveis.Contains(nome) ? "***" : p.OriginalValue;
            depois[nome] = PropriedadesSensiveis.Contains(nome) ? "***" : p.CurrentValue;
        }
        return (antes, depois);
    }

    private static Dictionary<string, object?> Snapshot(EntityEntry entry, bool useOriginal)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var p in entry.Properties)
        {
            var nome = p.Metadata.Name;
            if (nome is "CriadoEm" or "AtualizadoEm") continue;
            var v = useOriginal ? p.OriginalValue : p.CurrentValue;
            dict[nome] = PropriedadesSensiveis.Contains(nome) ? "***" : v;
        }
        return dict;
    }
}
