namespace SynthesisWire.Domain;

public sealed class Claim
{
    private readonly List<Evidence> _evidence = [];
    private Claim() { }

    internal Claim(Guid researchItemId, string statement, ClaimType claimType, Confidence confidence,
        IEnumerable<Evidence> evidence)
    {
        Id = Guid.CreateVersion7();
        ResearchItemId = researchItemId;
        Statement = Guard.Text(statement, nameof(statement));
        ClaimType = claimType;
        Confidence = confidence;
        var citations = evidence.ToArray();
        if (citations.Any(entry => entry.ClaimId != Guid.Empty)
            || citations.Select(entry => entry.Id).Distinct().Count() != citations.Length)
            throw new EditorialRuleException("Evidence must be unused and unique within a claim.");
        foreach (var entry in citations)
        {
            entry.AttachTo(Id);
            _evidence.Add(entry);
        }
    }

    public Guid Id { get; private set; }
    public Guid ResearchItemId { get; private set; }
    public string Statement { get; private set; } = string.Empty;
    public ClaimType ClaimType { get; private set; }
    public Confidence Confidence { get; private set; }
    public IReadOnlyCollection<Evidence> Evidence => _evidence.AsReadOnly();

    internal void RestoreEvidence(IEnumerable<Evidence> evidence)
    {
        if (evidence.Any(x => x.ClaimId != Id)) throw new InvalidOperationException("Persisted evidence ownership is invalid.");
        _evidence.AddRange(evidence);
    }
}

public sealed class Evidence
{
    private Evidence() { }

    public Evidence(SourceDocument sourceDocument, EvidenceKind kind, string excerpt, string locator)
    {
        ArgumentNullException.ThrowIfNull(sourceDocument);
        Id = Guid.CreateVersion7();
        SourceDocument = sourceDocument;
        SourceDocumentId = sourceDocument.Id;
        Kind = kind;
        Excerpt = Guard.Text(excerpt, nameof(excerpt));
        Locator = Guard.Text(locator, nameof(locator));
    }

    public Guid Id { get; private set; }
    public Guid ClaimId { get; private set; }
    public Guid SourceDocumentId { get; private set; }
    public SourceDocument SourceDocument { get; private set; } = null!;
    public EvidenceKind Kind { get; private set; }
    public string Excerpt { get; private set; } = string.Empty;
    public string Locator { get; private set; } = string.Empty;

    internal void RestoreSource(SourceDocument source)
    {
        if (source.Id != SourceDocumentId) throw new InvalidOperationException("Persisted source identity is invalid.");
        SourceDocument = source;
    }

    internal void AttachTo(Guid claimId)
    {
        if (ClaimId != Guid.Empty) throw new EditorialRuleException("Evidence already belongs to a claim.");
        ClaimId = claimId;
    }
}
