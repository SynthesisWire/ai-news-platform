using System.Runtime.CompilerServices;
using AiResearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Pgvector.EntityFrameworkCore;

namespace AiResearch.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")))
            Skip = "Set TEST_POSTGRES_CONNECTION_STRING to PostgreSQL with pgvector available and a test role allowed to CREATE DATABASE.";
    }
}

// Each test creates a uniquely named database. The supplied/source database is never modified or deleted.
public sealed class PostgresFixture : IAsyncDisposable
{
    private readonly string _baseConnectionString;
    private readonly string _database = $"airesearch_test_{Guid.NewGuid():N}";
    private bool _created;
    public string ConnectionString { get; }

    public PostgresFixture()
    {
        _baseConnectionString = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException("TEST_POSTGRES_CONNECTION_STRING is required.");
        ConnectionString = new NpgsqlConnectionStringBuilder(_baseConnectionString)
        {
            Database = _database,
            SearchPath = "public",
            Pooling = false
        }.ConnectionString;
    }

    public ResearchDbContext CreateContext() => new(new DbContextOptionsBuilder<ResearchDbContext>()
        .UseNpgsql(ConnectionString, o => o.UseVector()).Options);

    public async Task InitializeAsync(bool migrate = true)
    {
        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{_database}\"";
        await command.ExecuteNonQueryAsync();
        _created = true;
        if (migrate)
        {
            await using var db = CreateContext();
            await db.Database.MigrateAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_created) return;
        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE \"{_database}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }
}
