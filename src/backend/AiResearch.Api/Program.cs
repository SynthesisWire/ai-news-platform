using AiResearch.Api;
using AiResearch.Application;
using AiResearch.Domain;
using AiResearch.Infrastructure;
using AiResearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

var migrate = args.Contains("--migrate", StringComparer.Ordinal);
var seedDemo = args.Contains("--seed-demo", StringComparer.Ordinal);
var hostArgs = args.Where(a => a is not ("--migrate" or "--seed-demo")).ToArray();
var builder = WebApplication.CreateBuilder(hostArgs);
builder.Services.AddDbContext<ResearchDbContext>((services, options) =>
{
    var connectionString = services.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
        ?? throw new InvalidOperationException("Set ConnectionStrings__Postgres. The API never creates or seeds a database on normal startup.");
    options.UseNpgsql(connectionString, postgres => postgres.UseVector());
});
builder.Services.AddScoped<IPublicResearchReader, PublicResearchReader>();
builder.Services.AddScoped<DemoSeeder>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks().AddCheck<DatabaseReadinessHealthCheck>("postgres", tags: ["ready"]);

var app = builder.Build();
if (string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Postgres")))
    throw new InvalidOperationException("Set ConnectionStrings__Postgres. The API never creates or seeds a database on normal startup.");
if (migrate || seedDemo)
{
    await using var scope = app.Services.CreateAsyncScope();
    if (migrate)
    {
        await scope.ServiceProvider.GetRequiredService<ResearchDbContext>().Database.MigrateAsync();
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
