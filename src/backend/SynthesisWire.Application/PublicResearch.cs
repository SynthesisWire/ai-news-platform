using System.Collections.Immutable;
using SynthesisWire.Domain;

namespace SynthesisWire.Application;

public record ResearchItemSummary(Guid Id, string Slug, string Locale, string Title, string Summary,
    string ItemType, string EvidenceStrength, DateTimeOffset PublishedAt, bool IsSynthetic,
    int Version, ImmutableArray<string> Topics);

public sealed record PublicationHistoryEntry(int Version, DateTimeOffset PublishedAt, string ChangeNote);

public sealed record ResearchItemDetail(Guid Id, string Slug, string Locale, string Title, string Summary,
    string ItemType, string EvidenceStrength, DateTimeOffset PublishedAt, bool IsSynthetic,
    int Version, ImmutableArray<string> Topics, string WhyItMatters, ImmutableArray<string> Limitations,
    ImmutableArray<ClaimSnapshot> Claims, ImmutableArray<PublicationHistoryEntry> Versions)
    : ResearchItemSummary(Id, Slug, Locale, Title, Summary, ItemType, EvidenceStrength, PublishedAt, IsSynthetic, Version, Topics);

public sealed record ResearchItemList(IReadOnlyList<ResearchItemSummary> Items);

public interface IPublicResearchReader
{
    Task<IReadOnlyList<ResearchItemSummary>> ListAsync(string locale, CancellationToken cancellationToken);
    Task<ResearchItemDetail?> GetAsync(string slug, string locale, CancellationToken cancellationToken);
}

public static class PublicResearchMapping
{
    public static ResearchItemSummary ToSummary(this PublicationSnapshot snapshot) => new(snapshot.Id,
        snapshot.Slug, snapshot.Locale, snapshot.Title, snapshot.Summary, snapshot.ItemType,
        snapshot.EvidenceStrength, snapshot.PublishedAt, snapshot.IsSynthetic, snapshot.Version, snapshot.Topics);

    public static ResearchItemDetail ToDetail(this PublicationSnapshot snapshot, IEnumerable<PublicationHistoryEntry> versions)
        => new(snapshot.Id, snapshot.Slug, snapshot.Locale, snapshot.Title, snapshot.Summary, snapshot.ItemType,
            snapshot.EvidenceStrength, snapshot.PublishedAt, snapshot.IsSynthetic, snapshot.Version, snapshot.Topics,
            snapshot.WhyItMatters, snapshot.Limitations, snapshot.Claims, [.. versions]);
}
