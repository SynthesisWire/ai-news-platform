using System.Text.Json;
using SynthesisWire.Domain;

namespace SynthesisWire.Infrastructure.Persistence;

// Persistence only: a relay/Service Bus adapter is deliberately not implemented yet.
public sealed class OutboxMessage
{
    private OutboxMessage() { }

    internal OutboxMessage(PublicationVersion publication)
    {
        Id = publication.Id;
        AggregateId = publication.ResearchItemId;
        CausationId = publication.OperationId;
        CorrelationId = publication.OperationId;
        EventType = "ContentPublished";
        SchemaVersion = 1;
        OccurredAt = publication.PublishedAt;
        Producer = "SynthesisWire.Publishing";
        PayloadJson = JsonSerializer.Serialize(new
        {
            publicationId = publication.Id,
            researchItemId = publication.ResearchItemId,
            publication.Slug,
            publication.Locale,
            publication.Version,
            publication.PublishedAt,
            isSynthetic = publication.ReadSnapshot().IsSynthetic
        }, PublicationVersion.JsonOptions);
    }

    public Guid Id { get; private set; }
    public Guid AggregateId { get; private set; }
    public Guid CausationId { get; private set; }
    public Guid CorrelationId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public int SchemaVersion { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Producer { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTimeOffset? ProcessedAt { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
}
