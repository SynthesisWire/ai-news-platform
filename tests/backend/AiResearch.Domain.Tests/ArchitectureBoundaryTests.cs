using AiResearch.Application;

namespace AiResearch.Domain.Tests;

public sealed class ArchitectureBoundaryTests
{
    [Fact]
    public void Domain_has_no_outward_or_framework_dependencies()
    {
        var references = typeof(ResearchItem).Assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty);
        Assert.DoesNotContain(references, name => name.StartsWith("AiResearch.", StringComparison.Ordinal)
            || IsInfrastructureFramework(name));
    }

    [Fact]
    public void Application_depends_inward_on_domain_only()
    {
        var references = typeof(IPublicResearchReader).Assembly.GetReferencedAssemblies().Select(x => x.Name ?? string.Empty).ToArray();
        Assert.Contains("AiResearch.Domain", references);
        Assert.DoesNotContain(references, name => (name.StartsWith("AiResearch.", StringComparison.Ordinal) && name != "AiResearch.Domain")
            || IsInfrastructureFramework(name));
    }

    private static bool IsInfrastructureFramework(string name) => name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
        || name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
        || name.StartsWith("Npgsql", StringComparison.Ordinal)
        || name.StartsWith("Pgvector", StringComparison.Ordinal);
}
