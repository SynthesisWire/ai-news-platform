using System.Text;
using System.Text.Json;
using Npgsql;
using SynthesisWire.Infrastructure.Ingestion;
using SynthesisWire.Infrastructure.Persistence;

// Test-only executable, never referenced by the API or included in its container.
// It supplies an in-memory synthetic capture, without changing production fetch policy.
if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true"
    || Environment.GetEnvironmentVariable("SYNTHESISWIRE_EDITORIAL_E2E") != "true")
    throw new InvalidOperationException("This fixture requires an explicitly enabled disposable CI environment.");
var connection = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connection)
    || connection != Environment.GetEnvironmentVariable("ConnectionStrings__Postgres"))
    throw new InvalidOperationException("The live stack and fixture must use the same disposable test database.");
await using var source = NpgsqlDataSource.Create(connection);
var workflow = new EditorialWorkflow(source, new ResearchStore(source), new SyntheticFetcher());
var draft = await workflow.IngestAsync($"https://example.org/synthetic-cli-{Guid.CreateVersion7():N}", Environment.UserName);
Console.WriteLine(JsonSerializer.Serialize(draft, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

internal sealed class SyntheticFetcher : ISourceFetcher
{
    public Task<SourceCapture> FetchAsync(string url, CancellationToken cancellationToken = default)
        => Task.FromResult(new SourceCapture(url, url, "text/plain",
            Encoding.UTF8.GetBytes("Synthetic CLI evidence only. No real research finding."), DateTimeOffset.UtcNow, IsSynthetic: true));
}
