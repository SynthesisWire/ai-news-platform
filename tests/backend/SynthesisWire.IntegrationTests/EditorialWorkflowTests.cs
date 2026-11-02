using System.Net;
using System.Net.Http.Json;
using System.Text;
using SynthesisWire.Application;
using SynthesisWire.Infrastructure;
using SynthesisWire.Infrastructure.Ingestion;
using Dapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SynthesisWire.IntegrationTests;

public sealed class EditorialWorkflowTests
{
    [PostgresFact]
    public async Task Ingest_human_draft_review_and_publish_is_atomic_bilingual_and_idempotent()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        var workflow = Workflow(fixture);
        var capture = await workflow.IngestAsync("https://example.org/synthetic", "test-editor");
        Assert.Equal(capture.Id, (await workflow.IngestAsync("https://example.org/synthetic", "test-editor")).Id);
        Assert.Equal(0, await fixture.CountAsync("publication_versions"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.ExecuteAsync(new("publish", DraftId: capture.Id), "test-editor"));
        var saved = Assert.IsType<DraftResult>(await workflow.ExecuteAsync(new("save", DraftId: capture.Id, Content: Content()), "test-editor"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.ExecuteAsync(new("submit", DraftId: saved.Id, ExpectedRevision: 0), "test-editor"));
        await workflow.ExecuteAsync(new("submit", DraftId: saved.Id, ExpectedRevision: 1), "test-editor");
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.ExecuteAsync(new("review", DraftId: saved.Id, ExpectedRevision: 1), "test-editor"));
        await workflow.ExecuteAsync(new("review", DraftId: saved.Id, ExpectedRevision: 1, Confirmed: true), "test-reviewer");
        await Task.WhenAll(workflow.ExecuteAsync(new("publish", DraftId: saved.Id, ExpectedRevision: 1), "test-editor"),
            workflow.ExecuteAsync(new("publish", DraftId: saved.Id, ExpectedRevision: 1), "test-editor"));
        Assert.Equal(2, await fixture.CountAsync("publication_versions"));
        Assert.Equal(2, await fixture.CountAsync("outbox_messages"));
        await using var app = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseEnvironment("Testing").ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = fixture.ConnectionString })));
        using var client = app.CreateClient();
        foreach (var locale in new[] { "en", "pt-BR" })
        {
            var detail = await client.GetFromJsonAsync<ResearchItemDetail>($"/api/research-items/human-reviewed-fixture?locale={locale}");
            Assert.NotNull(detail);
            Assert.Equal(locale == "en" ? "Synthetic human title" : "Título humano sintético", detail.Title);
            Assert.True(detail.IsSynthetic);
            Assert.Equal("Synthetic source only.", detail.Claims.Single().Evidence.Single().Excerpt);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync("/api/editorial", new { action = "publish" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/source-captures/{capture.CaptureId}")).StatusCode);
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        Assert.Equal(5, await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM editorial_events"));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => connection.ExecuteAsync("DELETE FROM source_captures"));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => connection.ExecuteAsync("DELETE FROM editorial_events"));
        Assert.Equal(7, capture.CaptureId.Version);
        Assert.Equal(7, capture.ResearchItemId.Version);
    }

    [PostgresFact]
    public async Task Revision_invalidates_review_and_rejects_invented_evidence_or_missing_rights()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        var workflow = Workflow(fixture);
        var capture = await workflow.IngestAsync("https://example.org/synthetic", "test-editor");
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.ExecuteAsync(new("save", DraftId: capture.Id, Content: Content() with { RightsConfirmed = false }), "test-editor"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.ExecuteAsync(new("save", DraftId: capture.Id, Content: Content() with { Claims = [new("Invented", "Not in the source", "Unknown")] }), "test-editor"));
        await workflow.ExecuteAsync(new("save", DraftId: capture.Id, Content: Content()), "test-editor");
        await workflow.ExecuteAsync(new("submit", DraftId: capture.Id, ExpectedRevision: 1), "test-editor");
        await workflow.ExecuteAsync(new("review", DraftId: capture.Id, ExpectedRevision: 1, Confirmed: true), "test-reviewer");
        await workflow.ExecuteAsync(new("revise", DraftId: capture.Id, ExpectedRevision: 1), "test-editor");
        await Assert.ThrowsAsync<InvalidOperationException>(() => workflow.ExecuteAsync(new("publish", DraftId: capture.Id, ExpectedRevision: 2), "test-editor"));
        Assert.Equal(0, await fixture.CountAsync("publication_versions"));
    }

    [PostgresFact]
    public async Task Changed_capture_versions_and_cross_URL_material_matches_are_retained_for_human_reconciliation()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        var fetcher = new FixtureFetcher();
        var workflow = new EditorialWorkflow(fixture.DataSource, fixture.Store, fetcher);
        var first = await workflow.IngestAsync("https://example.org/synthetic", "test-editor");
        var second = await workflow.IngestAsync("https://example.org/other", "test-editor");
        Assert.Equal(first.CaptureId, second.PossibleDuplicateCaptureId);
        fetcher.Text = "Synthetic source only.\nChanged fixture.";
        var changed = await workflow.IngestAsync("https://example.org/synthetic", "test-editor");
        Assert.NotEqual(first.CaptureId, changed.CaptureId);
        await using var connection = await fixture.DataSource.OpenConnectionAsync();
        Assert.Equal(3, await connection.ExecuteScalarAsync<int>("SELECT count(*) FROM source_captures"));
        Assert.True(await fixture.Migrator.IsCurrentAsync());
    }

    private static EditorialWorkflow Workflow(PostgresFixture fixture) => new(fixture.DataSource, fixture.Store, new FixtureFetcher());
    private static HumanDraft Content() => new("human-reviewed-fixture", "Synthetic canonical record", "Synthetic source", "Fictional publisher",
        DemoSeeder.FixtureTime, true, [new("This is a synthetic illustration, not a research finding.", "Synthetic source only.", "Fixture paragraph 1")],
        new("Synthetic human title", "Human-written synthetic summary.", "Exercises a manual review gate.", ["Synthetic content only."]),
        new("Título humano sintético", "Resumo sintético escrito por pessoa.", "Verifica revisão manual.", ["Conteúdo somente sintético."]));
    private sealed class FixtureFetcher : ISourceFetcher
    {
        public string Text { get; set; } = "Synthetic source only.";
        public Task<SourceCapture> FetchAsync(string url, CancellationToken cancellationToken = default)
            => Task.FromResult(new SourceCapture(url, url, "text/plain", Encoding.UTF8.GetBytes(Text), DateTimeOffset.UtcNow, IsSynthetic: true));
    }
}
