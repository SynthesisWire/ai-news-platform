namespace SynthesisWire.Domain.Tests;

public sealed class UuidTests
{
    [Fact]
    public void New_entity_and_publication_ids_are_unique_version_seven()
    {
        var generated = new HashSet<Guid>();
        for (var index = 0; index < 256; index++)
        {
            var source = new SourceDocument(Guid.CreateVersion7(), "Synthetic source", "https://example.org/fixture",
                "Fictional publisher", DateTimeOffset.UtcNow, new string('a', 64), "fixture://uuid-test", true);
            var item = new ResearchItem(Guid.CreateVersion7(), $"uuid-fixture-{index}", "Synthetic record",
                ResearchItemType.ResearchPaper, EvidenceStrength.Synthetic, true, ["fixture"], DateTimeOffset.UtcNow);
            var evidence = new Evidence(source, EvidenceKind.SyntheticFixture, "Synthetic evidence", "Fixture");
            item.AddClaim("Synthetic claim", ClaimType.SyntheticIllustration, Confidence.Low, evidence);
            item.SetLocalization("en", "Synthetic title", "Synthetic summary", "Tests identifiers", "Synthetic only");
            item.SubmitForReview();
            item.ReviewLocalization("en");
            item.Approve();
            var publication = item.Publish("en", Guid.CreateVersion7(), DateTimeOffset.UtcNow, "Synthetic fixture");
            foreach (var id in new[] { source.Id, item.Id, evidence.Id, item.Claims.Single().Id,
                item.Localizations.Single().Id, publication.Id, publication.OperationId })
            {
                Assert.Equal(7, id.Version);
                Assert.True(generated.Add(id));
            }
        }
    }
}
