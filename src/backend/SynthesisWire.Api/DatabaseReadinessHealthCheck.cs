using SynthesisWire.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SynthesisWire.Api;

public sealed class DatabaseReadinessHealthCheck(SqlMigrator migrator) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await migrator.IsCurrentAsync(cancellationToken) ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database schema is missing, pending, or has drifted.");
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Database is not ready.");
        }
    }
}
