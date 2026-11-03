namespace SynthesisWire.Domain;

public sealed class Localization
{
    private Localization() { }

    internal Localization(Guid researchItemId, string locale, string title, string summary,
        string whyItMatters, IEnumerable<string> limitations)
    {
        Id = Guid.CreateVersion7();
        ResearchItemId = researchItemId;
        Locale = SupportedLocales.Normalize(locale);
        Revise(title, summary, whyItMatters, limitations);
    }

    public Guid Id { get; private set; }
    public Guid ResearchItemId { get; private set; }
    public string Locale { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string WhyItMatters { get; private set; } = string.Empty;
    private string[] _limitations = [];
    public string[] Limitations { get => [.. _limitations]; private set => _limitations = value; }
    public bool IsReviewed { get; private set; }

    internal void Revise(string title, string summary, string whyItMatters, IEnumerable<string> limitations)
    {
        var validTitle = Guard.Text(title, nameof(title));
        var validSummary = Guard.Text(summary, nameof(summary));
        var validWhyItMatters = Guard.Text(whyItMatters, nameof(whyItMatters));
        var validLimitations = limitations.Select(x => Guard.Text(x, nameof(limitations))).ToArray();
        if (validLimitations.Length == 0) throw new EditorialRuleException("Document at least one limitation.");
        Title = validTitle;
        Summary = validSummary;
        WhyItMatters = validWhyItMatters;
        Limitations = validLimitations;
        IsReviewed = false;
    }

    internal void Review() => IsReviewed = true;
    internal void RequireReview() => IsReviewed = false;
}
