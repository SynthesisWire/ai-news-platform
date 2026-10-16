using System.Text.RegularExpressions;

namespace SynthesisWire.Domain;

public sealed partial class ResearchItem
{
    private readonly List<Claim> _claims = [];
    private readonly List<Localization> _localizations = [];
    private readonly List<PublicationVersion> _publicationVersions = [];
    private ResearchItem() { }

    public ResearchItem(Guid id, string slug, string canonicalTitle, ResearchItemType itemType,
        EvidenceStrength evidenceStrength, bool isSynthetic, IEnumerable<string> topics, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new EditorialRuleException("Research item id is required.");
        if (!SlugPattern().IsMatch(slug)) throw new EditorialRuleException("Use a lowercase hyphenated slug.");
        Id = id;
        Slug = slug;
        CanonicalTitle = Guard.Text(canonicalTitle, nameof(canonicalTitle));
        ItemType = itemType;
        EvidenceStrength = evidenceStrength;
        IsSynthetic = isSynthetic;
        Topics = topics.Select(x => Guard.Text(x, nameof(topics))).Distinct().ToArray();
        CreatedAt = Guard.Utc(createdAt, nameof(createdAt));
        Status = EditorialStatus.Draft;
    }

    public Guid Id { get; private set; }
    public string Slug { get; private set; } = string.Empty;
    public string CanonicalTitle { get; private set; } = string.Empty;
    public ResearchItemType ItemType { get; private set; }
    public EvidenceStrength EvidenceStrength { get; private set; }
    public EditorialStatus Status { get; private set; }
    public bool IsSynthetic { get; private set; }
    private string[] _topics = [];
    public string[] Topics { get => [.. _topics]; private set => _topics = value; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyCollection<Claim> Claims => _claims.AsReadOnly();
    public IReadOnlyCollection<Localization> Localizations => _localizations.AsReadOnly();
    public IReadOnlyCollection<PublicationVersion> PublicationVersions => _publicationVersions.AsReadOnly();

    // Reconstitution of persisted state is separate from editorial commands.
    internal void RestoreChildren(IEnumerable<Claim> claims, IEnumerable<Localization> localizations, IEnumerable<PublicationVersion> publications)
    {
        if (claims.Any(x => x.ResearchItemId != Id) || localizations.Any(x => x.ResearchItemId != Id) || publications.Any(x => x.ResearchItemId != Id))
            throw new InvalidOperationException("Persisted aggregate ownership is invalid.");
        _claims.AddRange(claims);
        _localizations.AddRange(localizations);
        _publicationVersions.AddRange(publications);
    }

    public void AddClaim(string statement, ClaimType claimType, Confidence confidence, params Evidence[] evidence)
    {
        RequireDraft();
        _claims.Add(new Claim(Id, statement, claimType, confidence, evidence));
    }

    public void SetLocalization(string locale, string title, string summary, string whyItMatters, params string[] limitations)
    {
        RequireDraft();
        locale = SupportedLocales.Normalize(locale);
        var existing = _localizations.SingleOrDefault(x => x.Locale == locale);
        if (existing is null) _localizations.Add(new Localization(Id, locale, title, summary, whyItMatters, limitations));
        else existing.Revise(title, summary, whyItMatters, limitations);
    }

    public void SubmitForReview()
    {
        RequireDraft();
        EnsureEvidence();
        if (_localizations.Count == 0) throw new EditorialRuleException("At least one localization is required.");
        Status = EditorialStatus.InReview;
    }

    public void ReviewLocalization(string locale)
    {
        if (Status != EditorialStatus.InReview) throw new EditorialRuleException("Item must be in review.");
        GetLocalization(locale).Review();
    }

    public void Approve()
    {
        if (Status != EditorialStatus.InReview) throw new EditorialRuleException("Item must be in review.");
        EnsureEvidence();
        if (!_localizations.Any(x => x.IsReviewed)) throw new EditorialRuleException("At least one locale must be reviewed.");
        Status = EditorialStatus.Approved;
    }

    public void BeginRevision()
    {
        if (Status is not (EditorialStatus.Published or EditorialStatus.InReview or EditorialStatus.Approved))
            throw new EditorialRuleException("Only reviewed or published items can return to draft.");
        foreach (var localization in _localizations) localization.RequireReview();
        Status = EditorialStatus.Draft;
    }

    public PublicationVersion Publish(string locale, Guid operationId, DateTimeOffset publishedAt, string changeNote)
    {
        locale = SupportedLocales.Normalize(locale);
        if (operationId == Guid.Empty) throw new EditorialRuleException("Publication operation id is required.");
        var previous = _publicationVersions.SingleOrDefault(x => x.OperationId == operationId);
        if (previous is not null)
        {
            if (previous.Locale != locale || previous.ChangeNote != changeNote.Trim())
                throw new EditorialRuleException("An operation id cannot be reused for a different publication request.");
            return previous;
        }
        if (Status is not (EditorialStatus.Approved or EditorialStatus.Published))
            throw new EditorialRuleException("Editorial approval is required before publication.");
        EnsureEvidence();
        var localization = GetLocalization(locale);
        if (!localization.IsReviewed) throw new EditorialRuleException("The requested locale has not been reviewed.");
        var version = _publicationVersions.Where(x => x.Locale == locale).Select(x => x.Version).DefaultIfEmpty().Max() + 1;
        var publication = new PublicationVersion(this, localization, version, operationId, publishedAt, changeNote);
        _publicationVersions.Add(publication);
        Status = EditorialStatus.Published;
        return publication;
    }

    private Localization GetLocalization(string locale) => _localizations.SingleOrDefault(x => x.Locale == SupportedLocales.Normalize(locale))
        ?? throw new EditorialRuleException("The requested localization does not exist.");

    private void EnsureEvidence()
    {
        if (_claims.Count == 0 || _claims.Any(c => c.Evidence.Count == 0))
            throw new EditorialRuleException("Every public claim must have at least one evidence citation.");
        if (!IsSynthetic && _claims.SelectMany(c => c.Evidence).Any(e => e.SourceDocument.IsSynthetic || e.Kind == EvidenceKind.SyntheticFixture))
            throw new EditorialRuleException("Synthetic evidence cannot support a real research item.");
    }

    private void RequireDraft()
    {
        if (Status != EditorialStatus.Draft) throw new EditorialRuleException("Return the item to draft before editing.");
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();
}
