using System.Collections.Immutable;
using System.Text.Json;

namespace SynthesisWire.Domain;

public sealed record SourceSnapshot(Guid Id, string Title, string Url, string Publisher,
    DateTimeOffset PublishedAt, bool IsSynthetic);
public sealed record EvidenceSnapshot(Guid Id, string Kind, string Excerpt, string Locator, SourceSnapshot Source);
public sealed record ClaimSnapshot(Guid Id, string Statement, string ClaimType, string Confidence,
    ImmutableArray<EvidenceSnapshot> Evidence);
public sealed record PublicationSnapshot(Guid Id, string Slug, string Locale, string Title, string Summary,
    string ItemType, string EvidenceStrength, DateTimeOffset PublishedAt, bool IsSynthetic, int Version,
    ImmutableArray<string> Topics, string WhyItMatters, ImmutableArray<string> Limitations,
    ImmutableArray<ClaimSnapshot> Claims);

public static class PublicVocabulary
{
    public static string Format<T>(T value) where T : Enum => JsonNamingPolicy.KebabCaseLower.ConvertName(value.ToString());
}

// The JSON payload freezes all public text, claims, evidence and source metadata at publication.
public sealed class PublicationVersion
{
    private PublicationVersion() { }

    internal PublicationVersion(ResearchItem item, Localization localization, int version,
        Guid operationId, DateTimeOffset publishedAt, string changeNote)
    {
        Id = Guid.CreateVersion7();
        ResearchItemId = item.Id;
        Slug = item.Slug;
        Locale = localization.Locale;
        Version = version;
        OperationId = operationId;
        PublishedAt = Guard.Utc(publishedAt, nameof(publishedAt));
        ChangeNote = Guard.Text(changeNote, nameof(changeNote));
        var snapshot = new PublicationSnapshot(item.Id, item.Slug, Locale, localization.Title,
            localization.Summary, PublicVocabulary.Format(item.ItemType), PublicVocabulary.Format(item.EvidenceStrength),
            PublishedAt, item.IsSynthetic, Version, [.. item.Topics], localization.WhyItMatters,
            [.. localization.Limitations], [.. item.Claims.Select(c => new ClaimSnapshot(c.Id, c.Statement,
                PublicVocabulary.Format(c.ClaimType), PublicVocabulary.Format(c.Confidence),
                [.. c.Evidence.Select(e => new EvidenceSnapshot(e.Id, PublicVocabulary.Format(e.Kind),
                    e.Excerpt, e.Locator, new SourceSnapshot(e.SourceDocument.Id, e.SourceDocument.Title,
                        e.SourceDocument.Url, e.SourceDocument.Publisher, e.SourceDocument.PublishedAt,
                        e.SourceDocument.IsSynthetic)))]))]);
        SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions);
    }

    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web);
    public Guid Id { get; private set; }
    public Guid ResearchItemId { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string Locale { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public Guid OperationId { get; private set; }
    public DateTimeOffset PublishedAt { get; private set; }
    public string ChangeNote { get; private set; } = string.Empty;
    public string SnapshotJson { get; private set; } = string.Empty;

    public PublicationSnapshot ReadSnapshot() => JsonSerializer.Deserialize<PublicationSnapshot>(SnapshotJson, JsonOptions)
        ?? throw new InvalidOperationException("Publication snapshot is invalid.");
}
