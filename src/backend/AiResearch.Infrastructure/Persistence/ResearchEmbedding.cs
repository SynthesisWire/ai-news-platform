using Pgvector;

namespace AiResearch.Infrastructure.Persistence;

// Storage boundary only. No embedding provider or semantic query service is wired in this foundation.
public sealed class ResearchEmbedding
{
    public Guid ResearchItemId { get; set; }
    public string Model { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }
}
