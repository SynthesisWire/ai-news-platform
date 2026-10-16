using AiResearch.Application;
using AiResearch.Domain;
using AiResearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiResearch.Infrastructure;

public sealed class PublicResearchReader(ResearchDbContext db) : IPublicResearchReader
{
    public async Task<IReadOnlyList<ResearchItemSummary>> ListAsync(string locale, CancellationToken cancellationToken)
    {
        locale = SupportedLocales.Normalize(locale);
        // The feed is bounded to the latest 100 publications. Draft changes never replace this read model.
        var publications = await db.PublicationVersions.AsNoTracking()
            .Where(p => p.Locale == locale && !db.PublicationVersions.Any(newer =>
                newer.ResearchItemId == p.ResearchItemId && newer.Locale == p.Locale && newer.Version > p.Version))
            .OrderByDescending(p => p.PublishedAt).ThenBy(p => p.Slug).Take(100).ToListAsync(cancellationToken);
        return publications.Select(p => p.ReadSnapshot().ToSummary()).ToArray();
    }

    public async Task<ResearchItemDetail?> GetAsync(string slug, string locale, CancellationToken cancellationToken)
    {
        locale = SupportedLocales.Normalize(locale);
        var publications = await db.PublicationVersions.AsNoTracking()
            .Where(p => p.Slug == slug && p.Locale == locale)
            .OrderByDescending(p => p.Version).ToListAsync(cancellationToken);
        return publications.Count == 0 ? null : publications[0].ReadSnapshot().ToDetail(
            publications.Select(p => new PublicationHistoryEntry(p.Version, p.PublishedAt, p.ChangeNote)));
    }
}
