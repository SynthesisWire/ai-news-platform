using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pgvector.EntityFrameworkCore;

namespace AiResearch.Infrastructure.Persistence;

public sealed class ResearchDbContextFactory : IDesignTimeDbContextFactory<ResearchDbContext>
{
    public ResearchDbContext CreateDbContext(string[] args)
    {
        // Migration generation does not open this connection; updates require an explicitly supplied connection string.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Database=airesearch;Username=airesearch;Password=design-time-only";
        return new ResearchDbContext(new DbContextOptionsBuilder<ResearchDbContext>()
            .UseNpgsql(connection, o => o.UseVector()).Options);
    }
}
