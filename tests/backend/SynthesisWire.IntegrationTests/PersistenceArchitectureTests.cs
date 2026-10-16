using SynthesisWire.Infrastructure.Persistence;

namespace SynthesisWire.IntegrationTests;

public sealed class PersistenceArchitectureTests
{
    [Fact]
    public void Persistence_uses_Dapper_and_Npgsql_without_entity_framework()
    {
        var names = typeof(ResearchStore).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        Assert.Contains("Dapper", names);
        Assert.Contains("Npgsql", names);
        Assert.DoesNotContain(names, name => name?.Contains("EntityFramework", StringComparison.Ordinal) == true);
        Assert.NotEmpty(SqlMigrator.Migrations);
        Assert.Equal(SqlMigrator.Migrations.Count, SqlMigrator.Migrations.Select(x => x.Version).Distinct().Count());
    }
}
