using System.Net;
using System.Net.Http.Json;
using SynthesisWire.Application;
using SynthesisWire.Domain;
using SynthesisWire.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;


namespace SynthesisWire.IntegrationTests;

public sealed class PublicApiTests
{
    [PostgresFact]
    public async Task Public_api_returns_both_locales_citations_and_history()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        await fixture.SeedAsync();
        await using var app = CreateApp(fixture);
        using var client = app.CreateClient();
        foreach (var locale in new[] { "en", "pt-BR" })
        {
            var feed = await client.GetFromJsonAsync<ResearchItemList>($"/api/research-items?locale={locale}");
            Assert.NotNull(feed);
            var summary = Assert.Single(feed.Items);
            Assert.Equal(locale, summary.Locale);
            Assert.True(summary.IsSynthetic);
            Assert.Equal(1, summary.Version);
            var detail = await client.GetFromJsonAsync<ResearchItemDetail>($"/api/research-items/{summary.Slug}?locale={locale}");
            Assert.NotNull(detail);
            Assert.Equal(summary.Title, detail.Title);
            var source = Assert.Single(Assert.Single(detail.Claims).Evidence).Source;
            Assert.True(source.IsSynthetic);
            Assert.Equal("example.org", new Uri(source.Url).Host);
            Assert.Single(detail.Versions);
            Assert.NotEmpty(detail.Limitations);
        }
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/research-items?locale=fr")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/research-items/{DemoSeeder.Slug}?locale=fr")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/research-items/does-not-exist?locale=en")).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.PostAsJsonAsync("/api/research-items", new { title = "Unsecured write" })).StatusCode);
    }

    [PostgresFact]
    public async Task Normal_startup_does_not_seed_and_draft_items_remain_private()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        {
            var item = new ResearchItem(Guid.CreateVersion7(), "unpublished-item", "Draft canonical title",
                ResearchItemType.ResearchPaper, EvidenceStrength.Limited, false, ["attention"], DemoSeeder.FixtureTime);
            await fixture.Store.SaveAsync(item);
        }
        await using var app = CreateApp(fixture);
        using var client = app.CreateClient();
        var feed = await client.GetFromJsonAsync<ResearchItemList>("/api/research-items");
        Assert.NotNull(feed);
        Assert.Empty(feed.Items);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/research-items/unpublished-item")).StatusCode);
        Assert.Equal(1, await fixture.CountAsync("research_items"));
        Assert.Equal(0, await fixture.CountAsync("publication_versions"));
    }

    [PostgresFact]
    public async Task Missing_published_locale_returns_404_without_language_fallback()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync();
        {
            await fixture.SeedAsync();
            var seeded = (await fixture.Store.GetAsync(DemoSeeder.ResearchItemId))!;
            var source = seeded.Claims.Single().Evidence.Single().SourceDocument;
            var item = new ResearchItem(Guid.CreateVersion7(), "english-only", "English-only fixture",
                ResearchItemType.ResearchNote, EvidenceStrength.Synthetic, true, ["fixture"], DemoSeeder.FixtureTime);
            item.AddClaim("Synthetic claim", ClaimType.SyntheticIllustration, Confidence.Low,
                new Evidence(source, EvidenceKind.SyntheticFixture, "Synthetic excerpt", "Fixture"));
            item.SetLocalization("en", "English-only fixture", "English summary", "Tests locale boundaries", "Synthetic only");
            item.SubmitForReview();
            item.ReviewLocalization("en");
            item.Approve();
            item.Publish("en", Guid.CreateVersion7(), DemoSeeder.FixtureTime, "English fixture");
            await fixture.Store.SaveAsync(item);
        }
        await using var app = CreateApp(fixture);
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/research-items/english-only?locale=en")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/api/research-items/english-only?locale=pt-BR")).StatusCode);
        var portugueseFeed = await client.GetFromJsonAsync<ResearchItemList>("/api/research-items?locale=pt-BR");
        Assert.NotNull(portugueseFeed);
        Assert.DoesNotContain(portugueseFeed.Items, x => x.Slug == "english-only");
    }

    [PostgresFact]
    public async Task Liveness_survives_missing_schema_while_readiness_fails_and_startup_does_not_migrate()
    {
        await using var fixture = new PostgresFixture();
        await fixture.InitializeAsync(migrate: false);
        await using var app = CreateApp(fixture);
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.GetAsync("/health/ready")).StatusCode);
    }

    private static WebApplicationFactory<Program> CreateApp(PostgresFixture fixture)
        => new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:Postgres"] = fixture.ConnectionString }));
        });
}
