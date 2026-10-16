using System.Security.Cryptography;
using System.Text;
using Dapper;
using Npgsql;

namespace SynthesisWire.Infrastructure.Persistence;

public sealed class SqlMigrator(NpgsqlDataSource dataSource)
{
    private const long LockId = 7306102027;
    private static readonly string[] Tables = ["research_items", "source_documents", "claims", "evidence", "localizations", "publication_versions", "outbox_messages", "research_embeddings"];
    public static IReadOnlyList<SqlMigration> Migrations { get; } = LoadMigrations();

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(@LockId)", new { LockId }, transaction, cancellationToken: cancellationToken));
        if (!await HasLedgerAsync(connection, transaction, cancellationToken))
        {
            var existing = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                "SELECT to_regclass('public.research_items') IS NOT NULL OR to_regclass('public.\"__EFMigrationsHistory\"') IS NOT NULL", transaction: transaction, cancellationToken: cancellationToken));
            if (existing) throw new InvalidOperationException("Untracked or legacy schema found. A reviewed data-preserving baseline is required; no automatic overwrite is allowed.");
            await connection.ExecuteAsync(new CommandDefinition("""
                CREATE TABLE schema_migrations (
                    version text PRIMARY KEY, checksum text NOT NULL, schema_checksum text NOT NULL,
                    applied_at timestamptz NOT NULL DEFAULT now()
                )
                """, transaction: transaction, cancellationToken: cancellationToken));
        }
        var applied = (await connection.QueryAsync<AppliedMigration>(new CommandDefinition(
            "SELECT version, checksum, schema_checksum AS \"SchemaChecksum\" FROM schema_migrations ORDER BY version", transaction: transaction, cancellationToken: cancellationToken))).ToArray();
        ValidateHistory(applied);
        if (applied.Length > 0 && applied[^1].SchemaChecksum != await SchemaChecksumAsync(connection, transaction, cancellationToken))
            throw new InvalidOperationException("Database schema drift detected; review manual changes before migrating.");
        foreach (var migration in Migrations.Skip(applied.Length))
        {
            await connection.ExecuteAsync(new CommandDefinition(migration.Sql, transaction: transaction, cancellationToken: cancellationToken));
            var schemaChecksum = await SchemaChecksumAsync(connection, transaction, cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO schema_migrations(version, checksum, schema_checksum) VALUES (@Version, @Checksum, @SchemaChecksum)",
                new { migration.Version, migration.Checksum, SchemaChecksum = schemaChecksum }, transaction, cancellationToken: cancellationToken));
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> IsCurrentAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        if (!await HasLedgerAsync(connection, transaction, cancellationToken)) return false;
        var applied = (await connection.QueryAsync<AppliedMigration>(new CommandDefinition(
            "SELECT version, checksum, schema_checksum AS \"SchemaChecksum\" FROM schema_migrations ORDER BY version", transaction: transaction, cancellationToken: cancellationToken))).ToArray();
        ValidateHistory(applied);
        return applied.Length == Migrations.Count && applied[^1].SchemaChecksum == await SchemaChecksumAsync(connection, transaction, cancellationToken);
    }

    private static void ValidateHistory(AppliedMigration[] applied)
    {
        if (applied.Length > Migrations.Count || applied.Where((row, index) =>
            row.Version != Migrations[index].Version || row.Checksum != Migrations[index].Checksum).Any())
            throw new InvalidOperationException("Migration history drift detected: unknown, reordered, or changed SQL migration.");
    }

    private static async Task<bool> HasLedgerAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
        => await connection.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT to_regclass('public.schema_migrations') IS NOT NULL", transaction: transaction, cancellationToken: cancellationToken));

    private static async Task<string> SchemaChecksumAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
    {
        // Capture relevant DDL, not table data. Missing/changed columns, constraints, indexes, triggers,
        // trigger functions and vector extension availability all affect readiness and migration checks.
        var definitions = await connection.QueryAsync<string>(new CommandDefinition("""
            SELECT definition FROM (
              SELECT 'column:' || c.relname || ':' || a.attname || ':' || format_type(a.atttypid,a.atttypmod) || ':' || a.attnotnull || ':' || coalesce(pg_get_expr(d.adbin,d.adrelid),'') AS definition
              FROM pg_class c JOIN pg_namespace n ON n.oid=c.relnamespace JOIN pg_attribute a ON a.attrelid=c.oid
              LEFT JOIN pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum
              WHERE n.nspname='public' AND c.relname=ANY(@Tables) AND a.attnum>0 AND NOT a.attisdropped
              UNION ALL SELECT 'constraint:' || c.relname || ':' || k.conname || ':' || pg_get_constraintdef(k.oid)
              FROM pg_constraint k JOIN pg_class c ON c.oid=k.conrelid JOIN pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='public' AND c.relname=ANY(@Tables)
              UNION ALL SELECT 'index:' || indexdef FROM pg_indexes WHERE schemaname='public' AND tablename=ANY(@Tables)
              UNION ALL SELECT 'trigger:' || pg_get_triggerdef(t.oid) || ':' || t.tgenabled::text
              FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace
              WHERE n.nspname='public' AND c.relname=ANY(@Tables) AND NOT t.tgisinternal
              UNION ALL SELECT 'function:' || pg_get_functiondef(p.oid) FROM pg_proc p JOIN pg_namespace n ON n.oid=p.pronamespace
              WHERE n.nspname='public' AND p.proname='reject_immutable_record_change'
              UNION ALL SELECT 'extension:' || extname FROM pg_extension WHERE extname='vector'
            ) objects ORDER BY definition COLLATE "C"
            """, new { Tables }, transaction, cancellationToken: cancellationToken));
        return Hash(string.Join("\n", definitions));
    }

    private static IReadOnlyList<SqlMigration> LoadMigrations()
    {
        var assembly = typeof(SqlMigrator).Assembly;
        return assembly.GetManifestResourceNames().Where(name => name.Contains(".SqlMigrations.", StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal).Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                // Normalize checkout line endings so Windows and Linux compute identical checksums.
                var sql = reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
                return new SqlMigration(name.Split(".SqlMigrations.")[1], sql, Hash(sql));
            }).ToArray();
    }
    private static string Hash(string value) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private sealed record AppliedMigration(string Version, string Checksum, string SchemaChecksum);
}

public sealed record SqlMigration(string Version, string Sql, string Checksum);
