using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjetoVarejo.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var opt = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=.\\SQLEXPRESS;Database=ProjetoVarejo;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;")
            .Options;
        return new AppDbContext(opt);
    }
}
