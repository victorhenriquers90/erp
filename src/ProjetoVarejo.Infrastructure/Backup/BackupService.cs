using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjetoVarejo.Infrastructure.Data;
using ProjetoVarejo.Shared;

namespace ProjetoVarejo.Infrastructure.Backup;

public class BackupService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public BackupService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<Result<string>> ExecutarAsync(string? pastaDestino = null)
    {
        try
        {
            var connStr = _config.GetConnectionString("Default")
                ?? throw new InvalidOperationException("ConnectionString não configurada.");
            var builder = new SqlConnectionStringBuilder(connStr);
            var dbName = builder.InitialCatalog;
            if (string.IsNullOrWhiteSpace(dbName))
                return Result.Falha<string>("Database não identificado na connection string.");

            var pasta = string.IsNullOrWhiteSpace(pastaDestino)
                ? Path.Combine(AppContext.BaseDirectory, "Backups")
                : pastaDestino;
            Directory.CreateDirectory(pasta);

            var arquivo = Path.Combine(pasta, $"{dbName}_{DateTime.Now:yyyyMMdd_HHmmss}.bak");

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = $"BACKUP DATABASE [{dbName}] TO DISK = @path WITH FORMAT, INIT, COMPRESSION, NAME = 'ProjetoVarejo backup';";
            cmd.Parameters.AddWithValue("@path", arquivo);
            cmd.CommandTimeout = 600;
            await cmd.ExecuteNonQueryAsync();

            LimparAntigos(pasta, manterUltimos: 20);
            return Result.Ok(arquivo);
        }
        catch (Exception ex)
        {
            return Result.Falha<string>($"Falha no backup: {ex.Message}");
        }
    }

    private static void LimparAntigos(string pasta, int manterUltimos)
    {
        var arquivos = new DirectoryInfo(pasta)
            .GetFiles("*.bak")
            .OrderByDescending(f => f.CreationTime)
            .ToList();
        foreach (var antigo in arquivos.Skip(manterUltimos))
        {
            try { antigo.Delete(); } catch { }
        }
    }
}
