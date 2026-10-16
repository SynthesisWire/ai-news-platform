using System.Security.Cryptography;
using System.Text;
using AiResearch.Domain;
using AiResearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiResearch.Infrastructure;

public sealed class DemoSeeder(ResearchDbContext db)
{
    public const string Slug = "synthetic-efficient-attention";
    public static readonly Guid ResearchItemId = Guid.Parse("9289c142-5793-4a56-b312-abd96acb2400");
    public static readonly DateTimeOffset FixtureTime = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
    private const string Excerpt = "SYNTHETIC FIXTURE: An imaginary attention method reduces memory use on a toy benchmark. No real experiment or research result is represented.";

    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        // A transaction-scoped advisory lock also makes concurrent explicit seed jobs idempotent.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(7306102026)", cancellationToken);
        if (await db.ResearchItems.AnyAsync(x => x.Id == ResearchItemId || x.Slug == Slug, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return false;
        }
        var source = new SourceDocument(Guid.Parse("de794de6-2610-4aad-9970-0a248a6a7a01"),
            "Synthetic attention study: demonstration source", "https://example.org/synthetic-attention-study",
            "Example Research Lab (fictional)", FixtureTime.AddDays(-1),
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Excerpt))),
            "fixture://synthetic-efficient-attention/v1", isSynthetic: true);
        var item = new ResearchItem(ResearchItemId, Slug, "Synthetic efficient-attention research example",
            ResearchItemType.ResearchPaper, EvidenceStrength.Synthetic, true,
            ["efficient-attention", "evaluation", "research-methods"], FixtureTime);
        item.AddClaim("An imaginary attention method uses less memory on a toy benchmark; this is a synthetic illustration, not a scientific finding.",
            ClaimType.SyntheticIllustration, Confidence.Low,
            new Evidence(source, EvidenceKind.SyntheticFixture, Excerpt, "Fixture section 1, illustrative claim"));
        item.SetLocalization("en", "Efficient attention, examined: a synthetic research example",
            "An illustrative research record shows how claims, citations and limitations stay connected. All findings and sources in this example are fictional.",
            "This fixture demonstrates evidence-first publishing and bilingual editorial review. It does not establish that any attention technique works.",
            "Synthetic content only: no real paper was fetched or evaluated.",
            "No independent replication, measured speedup or production result exists.");
        item.SetLocalization("pt-BR", "Atenção eficiente em análise: um exemplo de pesquisa sintético",
            "Um registro ilustrativo mostra como alegações, citações e limitações permanecem conectadas. Todos os resultados e fontes deste exemplo são fictícios.",
            "Este exemplo demonstra a publicação baseada em evidências e a revisão editorial bilíngue. Ele não comprova a eficácia de nenhuma técnica de atenção.",
            "Conteúdo sintético: nenhum artigo real foi coletado ou avaliado.",
            "Não existe replicação independente, ganho de desempenho medido ou resultado em produção.");
        item.SubmitForReview();
        item.ReviewLocalization("en");
        item.ReviewLocalization("pt-BR");
        item.Approve();
        item.Publish("en", Guid.Parse("d4955e95-d732-46a2-a108-7425cc14ab01"), FixtureTime, "Initial synthetic demonstration; no real research claims.");
        item.Publish("pt-BR", Guid.Parse("d4955e95-d732-46a2-a108-7425cc14ab02"), FixtureTime, "Demonstração sintética inicial; sem resultados de pesquisa reais.");
        db.ResearchItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
