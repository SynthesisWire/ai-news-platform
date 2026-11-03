using SynthesisWire.Api;
using SynthesisWire.Application;
using SynthesisWire.Domain;
using SynthesisWire.Infrastructure;
using SynthesisWire.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Npgsql;
using System.Text.Json;
using SynthesisWire.Infrastructure.Ingestion;

var migrate = args.Contains("--migrate", StringComparer.Ordinal);
var seedDemo = args.Contains("--seed-demo", StringComparer.Ordinal);
var editorialIndex = Array.IndexOf(args, "--editorial");
if (editorialIndex >= 0 && (args.Length != 2 || editorialIndex != 0))
    throw new InvalidOperationException("Use --editorial INPUT.json as a separate local administrative command.");
var hostArgs = editorialIndex >= 0 ? [] : args.Where(a => a is not ("--migrate" or "--seed-demo")).ToArray();
var builder = WebApplication.CreateBuilder(hostArgs);
builder.Services.AddSingleton(services =>
{
    var connection = services.GetRequiredService<IConfiguration>().GetConnectionString("Postgres");
    if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Set ConnectionStrings__Postgres explicitly.");
    return NpgsqlDataSource.Create(connection);
});
builder.Services.AddSingleton<SqlMigrator>();
builder.Services.AddScoped<ResearchStore>();
builder.Services.AddScoped<IPublicResearchReader, PublicResearchReader>();
builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessHealthCheck>("postgres", tags: ["ready"]);

var app = builder.Build();
if (string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Postgres")))
    throw new InvalidOperationException("Set ConnectionStrings__Postgres. The API never creates or seeds a database on normal startup.");
if (editorialIndex >= 0)
{
    var input = new FileInfo(args[1]);
    if (!input.Exists || input.Length > 65536) throw new InvalidOperationException("Editorial input must be a local JSON file of at most 64 KiB.");
    var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };
    var request = JsonSerializer.Deserialize<EditorialRequest>(await File.ReadAllTextAsync(input.FullName), options)
        ?? throw new InvalidOperationException("Invalid editorial request.");
    await using var scope = app.Services.CreateAsyncScope();
    var workflow = new EditorialWorkflow(scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>(),
        scope.ServiceProvider.GetRequiredService<ResearchStore>(),
        new SafeSourceFetcher((Environment.GetEnvironmentVariable("INGEST_ALLOWED_HOSTS") ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)));
    var result = await workflow.ExecuteAsync(request, Environment.UserName);
    Console.WriteLine(JsonSerializer.Serialize(result, options));
    return; // Never starts the HTTP listener for an administrative command.
}
if (migrate || seedDemo)
{
    await using var scope = app.Services.CreateAsyncScope();
    if (migrate)
    {
        await scope.ServiceProvider.GetRequiredService<SqlMigrator>().MigrateAsync();
        app.Logger.LogInformation("Database migrations applied.");
    }
    if (seedDemo)
    {
        var created = await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync();
        app.Logger.LogInformation("Synthetic demonstration seed {Result}.", created ? "created" : "already present");
    }
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.MapGet("/api/research-items", async (string? locale, IPublicResearchReader reader, CancellationToken cancellationToken) =>
{
    if (!SupportedLocales.TryNormalize(locale, out var normalized))
        return Results.Problem(statusCode: 400, title: "Unsupported locale", detail: "Use locale=en or locale=pt-BR.");
    return Results.Ok(new ResearchItemList(await reader.ListAsync(normalized, cancellationToken)));
});
app.MapGet("/api/research-items/{slug}", async (string slug, string? locale, IPublicResearchReader reader, CancellationToken cancellationToken) =>
{
    if (!SupportedLocales.TryNormalize(locale, out var normalized))
        return Results.Problem(statusCode: 400, title: "Unsupported locale", detail: "Use locale=en or locale=pt-BR.");
    var item = await reader.GetAsync(slug, normalized, cancellationToken);
    return item is null ? Results.Problem(statusCode: 404, title: "Published research item not found") : Results.Ok(item);
});

await app.RunAsync();

public partial class Program;
