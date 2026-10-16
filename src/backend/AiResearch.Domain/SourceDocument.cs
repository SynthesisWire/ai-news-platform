namespace AiResearch.Domain;

// A source version is append-only. A changed source creates another SourceDocument.
public sealed class SourceDocument
{
    private SourceDocument() { }

    public SourceDocument(Guid id, string title, string url, string publisher,
        DateTimeOffset publishedAt, string contentHash, string snapshotUri, bool isSynthetic)
    {
        if (id == Guid.Empty) throw new EditorialRuleException("Source id is required.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
            throw new EditorialRuleException("Source URL must be an absolute HTTP(S) URL.");
        if (contentHash.Length != 64 || !contentHash.All(Uri.IsHexDigit))
            throw new EditorialRuleException("A SHA-256 content hash is required.");
        Id = id;
        Title = Guard.Text(title, nameof(title));
        Url = uri.AbsoluteUri;
        Publisher = Guard.Text(publisher, nameof(publisher));
        PublishedAt = Guard.Utc(publishedAt, nameof(publishedAt));
        ContentHash = contentHash.ToLowerInvariant();
        SnapshotUri = Guard.Text(snapshotUri, nameof(snapshotUri));
        IsSynthetic = isSynthetic;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string Publisher { get; private set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public string SnapshotUri { get; private set; } = string.Empty;
    public bool IsSynthetic { get; private set; }
}
