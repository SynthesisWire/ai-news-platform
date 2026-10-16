using SynthesisWire.Application;
using SynthesisWire.Domain;
using SynthesisWire.Infrastructure.Persistence;
using Dapper;

namespace SynthesisWire.Infrastructure;

public sealed class PublicResearchReader(ResearchStore store) : IPublicResearchReader
{
    public async Task<IReadOnlyList<ResearchItemSummary>> ListAsync(string locale, CancellationToken cancellationToken)
    {
        locale = SupportedLocales.Normalize(locale);
        await using var connection = await store.OpenConnectionAsync(cancellationToken);
        var publications = await connection.QueryAsync<PublicationVersion>(new CommandDefinition(
            ResearchStore.PublicationColumns + """
             FROM publication_versions p WHERE p.locale=@Locale AND NOT EXISTS (
               SELECT 1 FROM publication_versions newer WHERE newer.research_item_id=p.research_item_id
                 AND newer.locale=p.locale AND newer.version>p.version)
             ORDER BY p.published_at DESC,p.slug LIMIT 100
            """, new { Locale = locale }, cancellationToken: cancellationToken));
        return publications.Select(p => p.ReadSnapshot().ToSummary()).ToArray();
    }

    public async Task<ResearchItemDetail?> GetAsync(string slug, string locale, CancellationToken cancellationToken)
    {
        locale = SupportedLocales.Normalize(locale);
        await using var connection = await store.OpenConnectionAsync(cancellationToken);
        var publications = (await connection.QueryAsync<PublicationVersion>(new CommandDefinition(
            ResearchStore.PublicationColumns + " FROM publication_versions WHERE slug=@Slug AND locale=@Locale ORDER BY version DESC",
            new { Slug = slug, Locale = locale }, cancellationToken: cancellationToken))).ToArray();
        return publications.Length == 0 ? null : publications[0].ReadSnapshot().ToDetail(
            publications.Select(p => new PublicationHistoryEntry(p.Version, p.PublishedAt, p.ChangeNote)));
    }
}
