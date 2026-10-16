using AiResearch.Domain;

namespace AiResearch.Domain.Tests;

public sealed class PublicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Unapproved_items_cannot_be_published()
    {
        var item = CreateDraft();
        Assert.Throws<EditorialRuleException>(() => item.Publish("en", Guid.NewGuid(), Now, "First version"));
        Assert.Empty(item.PublicationVersions);
    }

    [Fact]
    public void Every_claim_requires_evidence_before_review()
    {
        var item = CreateDraft(addEvidence: false);
        Assert.Throws<EditorialRuleException>(item.SubmitForReview);
        Assert.Equal(EditorialStatus.Draft, item.Status);
    }

    [Fact]
    public void Items_without_claims_cannot_enter_review()
    {
        var item = NewItem();
        item.SetLocalization("en", "Title", "Summary", "Why it matters", "Limitations");
        Assert.Throws<EditorialRuleException>(item.SubmitForReview);
    }

    [Fact]
    public void Requested_locale_must_be_reviewed()
    {
        var item = CreateDraft();
        item.SetLocalization("pt-BR", "Título", "Resumo", "Importância", "Limitações");
        item.SubmitForReview();
        item.ReviewLocalization("en");
        item.Approve();
        Assert.Throws<EditorialRuleException>(() => item.Publish("pt-BR", Guid.NewGuid(), Now, "Primeira versão"));
    }

    [Fact]
    public void Approval_requires_a_reviewed_locale()
    {
        var item = CreateDraft();
        item.SubmitForReview();
        Assert.Throws<EditorialRuleException>(item.Approve);
    }

    [Fact]
    public void Retry_with_same_operation_id_returns_original_publication()
    {
        var item = Approve(CreateDraft());
        var operationId = Guid.NewGuid();
        var first = item.Publish("en", operationId, Now, "First version");
        var repeated = item.Publish("EN", operationId, Now.AddHours(1), "First version");
        Assert.Same(first, repeated);
        Assert.Single(item.PublicationVersions);
        Assert.Equal(Now, repeated.PublishedAt);
    }

    [Fact]
    public void Operation_id_cannot_be_reused_for_a_different_request()
    {
        var item = Approve(CreateDraft());
        var operationId = Guid.NewGuid();
        item.Publish("en", operationId, Now, "First version");
        Assert.Throws<EditorialRuleException>(() => item.Publish("en", operationId, Now, "Different correction"));
        Assert.Throws<EditorialRuleException>(() => item.Publish("pt-BR", operationId, Now, "First version"));
    }

    [Fact]
    public void Corrections_create_new_snapshots_without_rewriting_history()
    {
        var item = Approve(CreateDraft());
        var first = item.Publish("en", Guid.NewGuid(), Now, "First version");
        var originalJson = first.SnapshotJson;
        item.BeginRevision();
        item.SetLocalization("en", "Corrected title", "Corrected summary", "Clearer explanation", "Still a limited example");
        Approve(item);
        var correction = item.Publish("en", Guid.NewGuid(), Now.AddDays(1), "Corrected explanation");
        Assert.Equal(originalJson, first.SnapshotJson);
        Assert.Equal("Original title", first.ReadSnapshot().Title);
        Assert.Equal("Corrected title", correction.ReadSnapshot().Title);
        Assert.Equal(2, correction.Version);
        Assert.Equal(first.ReadSnapshot().Claims[0].Evidence[0].Source, correction.ReadSnapshot().Claims[0].Evidence[0].Source);
    }

    [Fact]
    public void Starting_revision_invalidates_previous_locale_reviews()
    {
        var item = Approve(CreateDraft());
        item.Publish("en", Guid.NewGuid(), Now, "First version");
        item.BeginRevision();
        item.SubmitForReview();
        Assert.Throws<EditorialRuleException>(item.Approve);
    }

    [Fact]
    public void Published_content_cannot_be_edited_without_a_revision()
    {
        var item = Approve(CreateDraft());
        item.Publish("en", Guid.NewGuid(), Now, "First version");
        Assert.Throws<EditorialRuleException>(() => item.SetLocalization("en", "Changed", "Summary", "Why", "Limits"));
    }

    [Fact]
    public void Synthetic_sources_cannot_support_real_items()
    {
        var item = CreateDraft(isSynthetic: false);
        Assert.Throws<EditorialRuleException>(item.SubmitForReview);
    }

    [Theory]
    [InlineData(null, "en")]
    [InlineData("en", "en")]
    [InlineData("EN", "en")]
    [InlineData("pt-br", "pt-BR")]
    [InlineData("PT-BR", "pt-BR")]
    public void Supported_locales_are_normalized(string? input, string expected)
    {
        Assert.True(SupportedLocales.TryNormalize(input, out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("pt")]
    [InlineData("")]
    [InlineData(" en ")]
    public void Unsupported_locales_are_rejected(string input) => Assert.False(SupportedLocales.TryNormalize(input, out _));

    [Fact]
    public void Failed_localization_edit_leaves_previous_content_unchanged()
    {
        var item = CreateDraft();
        var locale = Assert.Single(item.Localizations);
        Assert.Throws<EditorialRuleException>(() => item.SetLocalization("en", "Changed title", "Changed summary", "Changed reason"));
        Assert.Equal("Original title", locale.Title);
        Assert.Equal("Original summary", locale.Summary);
        Assert.Equal("Demonstrates traceability", locale.WhyItMatters);
        Assert.Equal(new[] { "Synthetic only" }, locale.Limitations);
    }

    [Fact]
    public void Rejected_claim_does_not_partially_attach_evidence()
    {
        var item = CreateDraft();
        var usedEvidence = Assert.Single(Assert.Single(item.Claims).Evidence);
        var unused = new Evidence(usedEvidence.SourceDocument, EvidenceKind.SyntheticFixture, "Another excerpt", "Section 2");
        Assert.Throws<EditorialRuleException>(() => item.AddClaim("Rejected claim", ClaimType.SyntheticIllustration,
            Confidence.Low, unused, usedEvidence));
        Assert.Equal(Guid.Empty, unused.ClaimId);
        Assert.Single(item.Claims);
    }

    [Fact]
    public void Collection_properties_cannot_mutate_editorial_content()
    {
        var item = CreateDraft();
        item.Topics[0] = "tampered";
        Assert.Single(item.Localizations).Limitations[0] = "tampered";
        Assert.Equal("attention", item.Topics[0]);
        Assert.Equal("Synthetic only", Assert.Single(item.Localizations).Limitations[0]);
    }

    [Fact]
    public void Source_urls_must_be_http_or_https()
    {
        Assert.Throws<EditorialRuleException>(() => new SourceDocument(Guid.NewGuid(), "Title", "javascript:alert(1)",
            "Publisher", Now, new string('a', 64), "fixture://source", true));
    }

    private static ResearchItem NewItem(bool isSynthetic = true) => new(Guid.NewGuid(), "example-item", "Canonical title",
        ResearchItemType.ResearchPaper, EvidenceStrength.Synthetic, isSynthetic, ["attention"], Now);

    private static ResearchItem CreateDraft(bool addEvidence = true, bool isSynthetic = true)
    {
        var item = NewItem(isSynthetic);
        var source = new SourceDocument(Guid.NewGuid(), "Example source", "https://example.org/source", "Fictional lab",
            Now, new string('a', 64), "fixture://source", true);
        item.AddClaim("Illustrative claim", ClaimType.SyntheticIllustration, Confidence.Low,
            addEvidence ? [new Evidence(source, EvidenceKind.SyntheticFixture, "Illustrative excerpt", "Section 1")] : []);
        item.SetLocalization("en", "Original title", "Original summary", "Demonstrates traceability", "Synthetic only");
        return item;
    }

    private static ResearchItem Approve(ResearchItem item)
    {
        item.SubmitForReview();
        item.ReviewLocalization("en");
        item.Approve();
        return item;
    }
}
