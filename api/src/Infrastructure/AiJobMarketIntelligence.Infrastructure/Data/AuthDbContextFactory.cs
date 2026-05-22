using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiJobMarketIntelligence.Infrastructure.Data;

/// <summary>
/// Design-time factory so EF tooling (migrations) can create AuthDbContext
/// without requiring the API host to boot (and without needing JWT_KEY etc.).
/// </summary>
public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        // Prefer env var so tooling works everywhere.
        var cs = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                 ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                 ?? "Server=127.0.0.1;Port=3306;Database=AiJobMarketIntelligence;User=root;Password=CHANGE_ME";

        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseMySql(cs, ServerVersion.AutoDetect(cs),
            mySql => mySql.MigrationsAssembly("AiJobMarketIntelligence.Infrastructure"));

        return new AuthDbContext(optionsBuilder.Options);
    }
}
