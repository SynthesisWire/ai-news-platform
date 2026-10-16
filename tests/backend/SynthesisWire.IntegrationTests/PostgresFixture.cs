using System.Runtime.CompilerServices;
using SynthesisWire.Infrastructure;
using SynthesisWire.Infrastructure.Persistence;
using Dapper;
using Npgsql;

namespace SynthesisWire.IntegrationTests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")))
            Skip = "Set TEST_POSTGRES_CONNECTION_STRING to PostgreSQL with pgvector and a disposable test role allowed to CREATE DATABASE.";
    }
}

public sealed class PostgresFixture : IAsyncDisposable
{
    private readonly string _baseConnectionString;
    private readonly string _database = $"airesearch_test_{Guid.CreateVersion7():N}";
    private bool _created;
    public string ConnectionString { get; }
    public NpgsqlDataSource DataSource { get; }
    public ResearchStore Store { get; }
    public SqlMigrator Migrator { get; }
    public PostgresFixture()
    {
        _baseConnectionString = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")
            ?? throw new InvalidOperationException("TEST_POSTGRES_CONNECTION_STRING is required.");
        ConnectionString = new NpgsqlConnectionStringBuilder(_baseConnectionString) { Database = _database, SearchPath = "public", Pooling = false }.ConnectionString;
        DataSource = NpgsqlDataSource.Create(ConnectionString);
        Store = new ResearchStore(DataSource);
        Migrator = new SqlMigrator(DataSource);
    }
    public Task<bool> SeedAsync() => new DemoSeeder(DataSource, Store).SeedAsync();
    public async Task InitializeAsync(bool migrate = true)
    {
        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        // PostgreSQL identifiers cannot be bound: this identifier is generated internally, never supplied externally.
        command.CommandText = $"CREATE DATABASE \"{_database}\"";
        await command.ExecuteNonQueryAsync();
        _created = true;
        if (migrate) await Migrator.MigrateAsync();
    }
    public async Task<int> CountAsync(string table)
    {
        if (table is not ("research_items" or "source_documents" or "publication_versions" or "outbox_messages" or "schema_migrations"))
            throw new ArgumentException("Unknown test table.");
        await using var connection = await DataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int>($"SELECT count(*) FROM {table}");
    }
    public async ValueTask DisposeAsync()
    {
        await DataSource.DisposeAsync();
        if (!_created) return;
        await using var connection = new NpgsqlConnection(_baseConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE \"{_database}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync();
    }
}
