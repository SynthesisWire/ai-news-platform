using AiResearch.Domain;
using AiResearch.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AiResearch.IntegrationTests;

public sealed class PublicationPersistenceTests
{
    [PostgresFact]
    public async Task Migration_seed_and_repeated_seed_are_idempotent()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await using var db = fixture.CreateContext();
        Assert.False((await db.Database.GetPendingMigrationsAsync()).Any());
        Assert.True(await new DemoSeeder(db).SeedAsync());
        db.ChangeTracker.Clear();
        Assert.False(await new DemoSeeder(db).SeedAsync());
        Assert.Equal(1, await db.ResearchItems.CountAsync());
        Assert.Equal(1, await db.SourceDocuments.CountAsync());
        Assert.Equal(2, await db.PublicationVersions.CountAsync());
        Assert.Equal(2, await db.OutboxMessages.CountAsync());
        Assert.All(await db.OutboxMessages.ToListAsync(), message =>
        {
            Assert.Equal("ContentPublished", message.EventType);
            Assert.Equal(DemoSeeder.ResearchItemId, message.AggregateId);
            Assert.Equal(1, message.SchemaVersion);
            Assert.Null(message.ProcessedAt);
        });
        await db.Database.MigrateAsync();
        Assert.Equal(2, await db.PublicationVersions.CountAsync());
    }

    [PostgresFact]
    public async Task Concurrent_seed_jobs_create_only_one_fixture()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await using var first = fixture.CreateContext();
        await using var second = fixture.CreateContext();
        var results = await Task.WhenAll(new DemoSeeder(first).SeedAsync(), new DemoSeeder(second).SeedAsync());
        Assert.Single(results, created => created);
        Assert.Equal(2, await first.OutboxMessages.CountAsync());
    }

    [PostgresFact]
    public async Task Snapshot_read_is_unchanged_by_unpublished_revision()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await using var db = fixture.CreateContext();
        await new DemoSeeder(db).SeedAsync();
        db.ChangeTracker.Clear();
        var reader = new PublicResearchReader(db);
        var before = await reader.GetAsync(DemoSeeder.Slug, "en", default);
        var item = await db.ResearchItems.Include(x => x.Localizations).SingleAsync();
        item.BeginRevision();
        item.SetLocalization("en", "Unpublished title", "Unpublished summary", "Draft explanation", "Draft limitations");
        await db.SaveChangesAsync();
        var after = await reader.GetAsync(DemoSeeder.Slug, "en", default);
        Assert.NotNull(before);
        Assert.NotNull(after);
        Assert.Equal(before.Title, after.Title);
        Assert.Single(after.Versions);
        Assert.Equal(2, await db.OutboxMessages.CountAsync());
    }

    [PostgresFact]
    public async Task Rolled_back_publication_does_not_leave_outbox_rows()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await using (var db = fixture.CreateContext())
        {
            await new DemoSeeder(db).SeedAsync();
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var item = await db.ResearchItems.Include(x => x.Localizations).Include(x => x.PublicationVersions)
                .Include(x => x.Claims).ThenInclude(x => x.Evidence).ThenInclude(x => x.SourceDocument).SingleAsync();
            item.Publish("en", Guid.NewGuid(), DemoSeeder.FixtureTime.AddDays(1), "Correction transaction fixture");
            await db.SaveChangesAsync();
            Assert.Equal(3, await db.OutboxMessages.CountAsync());
            await transaction.RollbackAsync();
        }
        await using var check = fixture.CreateContext();
        Assert.Equal(2, await check.PublicationVersions.CountAsync());
        Assert.Equal(2, await check.OutboxMessages.CountAsync());
    }

    [PostgresFact]
    public async Task Published_versions_and_sources_reject_sql_mutation()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await using var db = fixture.CreateContext();
        await new DemoSeeder(db).SeedAsync();
        await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("UPDATE publication_versions SET change_note = 'tampered'"));
        await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM publication_versions"));
        await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync("UPDATE source_documents SET title = 'tampered'"));
        Assert.Equal(2, await db.PublicationVersions.CountAsync());
    }

    [PostgresFact]
    public async Task Persisted_publication_retry_does_not_enqueue_another_event()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await using var db = fixture.CreateContext();
        await new DemoSeeder(db).SeedAsync();
        db.ChangeTracker.Clear();
        var item = await db.ResearchItems.Include(x => x.PublicationVersions).SingleAsync();
        var original = item.PublicationVersions.Single(x => x.Locale == "en");
        var retry = item.Publish("en", original.OperationId, DemoSeeder.FixtureTime.AddHours(1), original.ChangeNote);
        Assert.Equal(original.Id, retry.Id);
        Assert.Equal(0, await db.SaveChangesAsync());
        Assert.Equal(2, await db.OutboxMessages.CountAsync());
    }

    [PostgresFact]
    public async Task Published_correction_is_latest_without_changing_old_or_other_locale_versions()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await using var db = fixture.CreateContext();
        await new DemoSeeder(db).SeedAsync();
        db.ChangeTracker.Clear();
        var item = await db.ResearchItems.Include(x => x.Localizations).Include(x => x.PublicationVersions)
            .Include(x => x.Claims).ThenInclude(x => x.Evidence).ThenInclude(x => x.SourceDocument).SingleAsync();
        var original = item.PublicationVersions.Single(x => x.Locale == "en");
        var originalJson = original.SnapshotJson;
        item.BeginRevision();
        item.SetLocalization("en", "Corrected fixture title", "Corrected fixture summary", "Clarifies the example", "Still fictional");
        item.SubmitForReview();
        item.ReviewLocalization("en");
        item.Approve();
        item.Publish("en", Guid.NewGuid(), DemoSeeder.FixtureTime.AddDays(1), "Clarified the synthetic example");
        await db.SaveChangesAsync();
        var reader = new PublicResearchReader(db);
        var feed = await reader.ListAsync("en", default);
        Assert.Equal(2, Assert.Single(feed).Version);
        var detail = await reader.GetAsync(DemoSeeder.Slug, "en", default);
        Assert.NotNull(detail);
        Assert.Equal("Corrected fixture title", detail.Title);
        Assert.Equal(new[] { 2, 1 }, detail.Versions.Select(x => x.Version));
        Assert.Equal(originalJson, (await db.PublicationVersions.SingleAsync(x => x.Id == original.Id)).SnapshotJson);
        Assert.Equal(1, Assert.Single(await reader.ListAsync("pt-BR", default)).Version);
        Assert.Equal(3, await db.OutboxMessages.CountAsync());
    }

    [PostgresFact]
    public async Task Pgvector_and_full_text_search_are_real_database_capabilities()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await using var db = fixture.CreateContext();
        await new DemoSeeder(db).SeedAsync();
        var distance = await db.Database.SqlQueryRaw<double>("SELECT '[1,0,0]'::vector <-> '[0,1,0]'::vector AS \"Value\"").SingleAsync();
        Assert.InRange(distance, 1.414, 1.415);
        var matches = await db.Database.SqlQueryRaw<int>("SELECT COUNT(*)::integer AS \"Value\" FROM research_items WHERE search_vector @@ plainto_tsquery('simple', 'attention')").SingleAsync();
        Assert.Equal(1, matches);
        Assert.False(db.Database.HasPendingModelChanges());
    }
}
