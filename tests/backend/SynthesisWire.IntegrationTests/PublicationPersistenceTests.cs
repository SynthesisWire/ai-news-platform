using SynthesisWire.Domain;
using SynthesisWire.Infrastructure;
using SynthesisWire.Infrastructure.Persistence;
using Dapper;
using Npgsql;

namespace SynthesisWire.IntegrationTests;

public sealed class PublicationPersistenceTests
{
    [PostgresFact]
    public async Task Migration_seed_and_repeated_seed_are_idempotent()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        Assert.True(await fixture.Migrator.IsCurrentAsync());
        Assert.True(await fixture.SeedAsync());
        Assert.False(await fixture.SeedAsync());
        Assert.Equal(1, await fixture.CountAsync("research_items"));
        Assert.Equal(1, await fixture.CountAsync("source_documents"));
        Assert.Equal(2, await fixture.CountAsync("publication_versions"));
        Assert.Equal(2, await fixture.CountAsync("outbox_messages"));
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        var messages = await connection.QueryAsync<OutboxMessage>("""
            SELECT id, aggregate_id AS "AggregateId", event_type AS "EventType", schema_version AS "SchemaVersion", processed_at AS "ProcessedAt"
            FROM outbox_messages
            """);
        Assert.All(messages, message =>
        {
            Assert.Equal("ContentPublished", message.EventType);
            Assert.Equal(DemoSeeder.ResearchItemId, message.AggregateId);
            Assert.Equal(1, message.SchemaVersion);
            Assert.Null(message.ProcessedAt);
            Assert.Equal(7, message.Id.Version);
        });
        await fixture.Migrator.MigrateAsync();
        Assert.Equal(2, await fixture.CountAsync("publication_versions"));
    }

    [PostgresFact]
    public async Task Concurrent_seed_jobs_create_only_one_fixture()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        var results = await Task.WhenAll(fixture.SeedAsync(), fixture.SeedAsync());
        Assert.Single(results, created => created);
        Assert.Equal(2, await fixture.CountAsync("outbox_messages"));
    }

    [PostgresFact]
    public async Task Snapshot_read_is_unchanged_by_unpublished_revision()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await fixture.SeedAsync();
        var reader = new PublicResearchReader(fixture.Store);
        var before = await reader.GetAsync(DemoSeeder.Slug, "en", default);
        var item = (await fixture.Store.GetAsync(DemoSeeder.ResearchItemId))!;
        item.BeginRevision();
        item.SetLocalization("en", "Unpublished title", "Unpublished summary", "Draft explanation", "Draft limitations");
        await fixture.Store.SaveAsync(item);
        var after = await reader.GetAsync(DemoSeeder.Slug, "en", default);
        Assert.NotNull(before);
        Assert.NotNull(after);
        Assert.Equal(before.Title, after.Title);
        Assert.Single(after.Versions);
        Assert.Equal(2, await fixture.CountAsync("outbox_messages"));
    }

    [PostgresFact]
    public async Task Rolled_back_publication_does_not_leave_outbox_rows()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await fixture.SeedAsync();
        var item = (await fixture.Store.GetAsync(DemoSeeder.ResearchItemId))!;
        item.Publish("en", Guid.CreateVersion7(), DemoSeeder.FixtureTime.AddDays(1), "Correction transaction fixture");
        await using (var connection = await fixture.DataSource.OpenConnectionAsync())
        {
            await using var transaction = await connection.BeginTransactionAsync();
            await fixture.Store.SaveAsync(item, connection, transaction);
            Assert.Equal(3, await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM outbox_messages", transaction: transaction));
            await transaction.RollbackAsync();
        }
        Assert.Equal(2, await fixture.CountAsync("publication_versions"));
        Assert.Equal(2, await fixture.CountAsync("outbox_messages"));
    }

    [PostgresFact]
    public async Task Published_versions_and_sources_reject_sql_mutation()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await fixture.SeedAsync();
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync("UPDATE publication_versions SET change_note = 'tampered'"));
        await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync("DELETE FROM publication_versions"));
        await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync("UPDATE source_documents SET title = 'tampered'"));
        await Assert.ThrowsAsync<PostgresException>(() => connection.ExecuteAsync("DELETE FROM source_documents"));
        Assert.Equal(2, await fixture.CountAsync("publication_versions"));
    }

    [PostgresFact]
    public async Task Persisted_publication_retry_does_not_enqueue_another_event()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await fixture.SeedAsync();
        var item = (await fixture.Store.GetAsync(DemoSeeder.ResearchItemId))!;
        var original = item.PublicationVersions.Single(x => x.Locale == "en");
        var retry = item.Publish("en", original.OperationId, DemoSeeder.FixtureTime.AddHours(1), original.ChangeNote);
        Assert.Equal(original.Id, retry.Id);
        Assert.Equal(0, await fixture.Store.SaveAsync(item));
        Assert.Equal(2, await fixture.CountAsync("outbox_messages"));
    }

    [PostgresFact]
    public async Task Published_correction_is_latest_without_changing_old_or_other_locale_versions()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await fixture.SeedAsync();
        var item = (await fixture.Store.GetAsync(DemoSeeder.ResearchItemId))!;
        var original = item.PublicationVersions.Single(x => x.Locale == "en");
        var originalJson = original.SnapshotJson;
        item.BeginRevision();
        item.SetLocalization("en", "Corrected fixture title", "Corrected fixture summary", "Clarifies the example", "Still fictional");
        item.SubmitForReview();
        item.ReviewLocalization("en");
        item.Approve();
        item.Publish("en", Guid.CreateVersion7(), DemoSeeder.FixtureTime.AddDays(1), "Clarified the synthetic example");
        await fixture.Store.SaveAsync(item);
        var reader = new PublicResearchReader(fixture.Store);
        Assert.Equal(2, Assert.Single(await reader.ListAsync("en", default)).Version);
        var detail = await reader.GetAsync(DemoSeeder.Slug, "en", default);
        Assert.NotNull(detail);
        Assert.Equal("Corrected fixture title", detail.Title);
        Assert.Equal(new[] { 2, 1 }, detail.Versions.Select(x => x.Version));
        Assert.Equal(originalJson, (await fixture.Store.GetAsync(item.Id))!.PublicationVersions.Single(x => x.Id == original.Id).SnapshotJson);
        Assert.Equal(1, Assert.Single(await reader.ListAsync("pt-BR", default)).Version);
        Assert.Equal(3, await fixture.CountAsync("outbox_messages"));
    }

    [PostgresFact]
    public async Task Pgvector_and_full_text_search_are_real_database_capabilities()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await fixture.SeedAsync();
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        var distance = await connection.ExecuteScalarAsync<double>("SELECT '[1,0,0]'::vector <-> '[0,1,0]'::vector");
        Assert.InRange(distance, 1.414, 1.415);
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM research_items WHERE search_vector @@ plainto_tsquery('simple', 'attention')"));
        Assert.True(await fixture.Migrator.IsCurrentAsync());
    }

    [PostgresFact]
    public async Task Migration_history_and_schema_drift_fail_clearly_without_overwriting_data()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await fixture.SeedAsync();
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("ALTER TABLE research_items ADD COLUMN unexpected text");
        Assert.False(await fixture.Migrator.IsCurrentAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Migrator.MigrateAsync());
        Assert.Equal(2, await fixture.CountAsync("publication_versions"));
        await connection.ExecuteAsync("ALTER TABLE research_items DROP COLUMN unexpected");
        Assert.True(await fixture.Migrator.IsCurrentAsync());
        await connection.ExecuteAsync("UPDATE schema_migrations SET checksum='tampered'");
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Migrator.MigrateAsync());
    }

    [PostgresFact]
    public async Task Concurrent_migrations_are_serialized_and_untracked_schema_is_preserved()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync(migrate: false);
        await Task.WhenAll(fixture.Migrator.MigrateAsync(), fixture.Migrator.MigrateAsync());
        Assert.Equal(SqlMigrator.Migrations.Count, await fixture.CountAsync("schema_migrations"));
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("DROP TABLE schema_migrations");
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Migrator.MigrateAsync());
        Assert.Equal(0, await fixture.CountAsync("research_items"));
    }

    [PostgresFact]
    public async Task Parameterized_public_reads_reject_injection_without_modifying_data()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await fixture.SeedAsync();
        var reader = new PublicResearchReader(fixture.Store);
        Assert.Null(await reader.GetAsync("'; DELETE FROM research_items; --", "en", default));
        Assert.Equal(1, await fixture.CountAsync("research_items"));
    }

    [PostgresFact]
    public async Task Failed_migration_rolls_back_all_new_schema_and_keeps_existing_tables()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync(migrate: false);
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("CREATE TABLE source_documents (sentinel integer NOT NULL); INSERT INTO source_documents VALUES (42)");
        await Assert.ThrowsAsync<PostgresException>(() => fixture.Migrator.MigrateAsync());
        Assert.False(await connection.ExecuteScalarAsync<bool>("SELECT to_regclass('public.research_items') IS NOT NULL OR to_regclass('public.schema_migrations') IS NOT NULL"));
        Assert.Equal(42, await connection.ExecuteScalarAsync<int>("SELECT sentinel FROM source_documents"));
    }
}
