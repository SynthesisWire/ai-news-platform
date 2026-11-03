using System.Data;
using SynthesisWire.Domain;
using Dapper;
using Npgsql;

namespace SynthesisWire.Infrastructure.Persistence;

public sealed class ResearchStore
{
    private readonly NpgsqlDataSource _dataSource;
    static ResearchStore() => SqlMapper.AddTypeHandler(new UtcTimestampHandler());
    public ResearchStore(NpgsqlDataSource dataSource) => _dataSource = dataSource;
    internal ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken) => _dataSource.OpenConnectionAsync(cancellationToken);

    public async Task<ResearchItem?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var item = await connection.QuerySingleOrDefaultAsync<ResearchItem>(new CommandDefinition("""
            SELECT id, slug, canonical_title AS "CanonicalTitle", item_type AS "ItemType",
              evidence_strength AS "EvidenceStrength", status, is_synthetic AS "IsSynthetic", topics, created_at AS "CreatedAt"
            FROM research_items WHERE id=@Id
            """, new { Id = id }, transaction, cancellationToken: cancellationToken));
        if (item is null) return null;
        var claims = (await connection.QueryAsync<Claim>(new CommandDefinition("""
            SELECT id, research_item_id AS "ResearchItemId", statement, claim_type AS "ClaimType", confidence
            FROM claims WHERE research_item_id=@Id ORDER BY id
            """, new { Id = id }, transaction, cancellationToken: cancellationToken))).ToArray();
        var evidence = (await connection.QueryAsync<Evidence>(new CommandDefinition("""
            SELECT e.id, e.claim_id AS "ClaimId", e.source_document_id AS "SourceDocumentId", e.kind, e.excerpt, e.locator
            FROM evidence e JOIN claims c ON c.id=e.claim_id WHERE c.research_item_id=@Id ORDER BY e.id
            """, new { Id = id }, transaction, cancellationToken: cancellationToken))).ToArray();
        var sources = (await connection.QueryAsync<SourceDocument>(new CommandDefinition("""
            SELECT DISTINCT s.id, s.title, s.url, s.publisher, s.published_at AS "PublishedAt",
              s.content_hash AS "ContentHash", s.snapshot_uri AS "SnapshotUri", s.is_synthetic AS "IsSynthetic"
            FROM source_documents s JOIN evidence e ON e.source_document_id=s.id JOIN claims c ON c.id=e.claim_id
            WHERE c.research_item_id=@Id
            """, new { Id = id }, transaction, cancellationToken: cancellationToken))).ToDictionary(x => x.Id);
        foreach (var entry in evidence) entry.RestoreSource(sources[entry.SourceDocumentId]);
        foreach (var claim in claims) claim.RestoreEvidence(evidence.Where(x => x.ClaimId == claim.Id));
        var localizations = await connection.QueryAsync<Localization>(new CommandDefinition("""
            SELECT id, research_item_id AS "ResearchItemId", locale, title, summary, why_it_matters AS "WhyItMatters",
              limitations, is_reviewed AS "IsReviewed" FROM localizations WHERE research_item_id=@Id ORDER BY locale
            """, new { Id = id }, transaction, cancellationToken: cancellationToken));
        var publications = await connection.QueryAsync<PublicationVersion>(new CommandDefinition(PublicationColumns +
            " FROM publication_versions WHERE research_item_id=@Id ORDER BY locale, version", new { Id = id }, transaction, cancellationToken: cancellationToken));
        item.RestoreChildren(claims, localizations, publications);
        await transaction.CommitAsync(cancellationToken);
        return item;
    }

    internal const string PublicationColumns = """
        SELECT id, research_item_id AS "ResearchItemId", slug, locale, version, operation_id AS "OperationId",
          published_at AS "PublishedAt", change_note AS "ChangeNote", snapshot_json::text AS "SnapshotJson"
        """;

    public async Task<int> SaveAsync(ResearchItem item, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var added = await SaveAsync(item, connection, transaction, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return added;
    }

    // The caller-owned transaction allows seed jobs and publication/outbox tests to share one atomic unit.
    public async Task<int> SaveAsync(ResearchItem item, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken = default)
    {
        if (transaction.Connection != connection) throw new ArgumentException("The transaction must own the supplied connection.");
        await connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(hashtextextended(@Id,0))",
            new { Id = item.Id.ToString() }, transaction, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO research_items(id,slug,canonical_title,item_type,evidence_strength,status,is_synthetic,topics,created_at)
            VALUES (@Id,@Slug,@CanonicalTitle,@ItemType,@EvidenceStrength,@Status,@IsSynthetic,@Topics,@CreatedAt)
            ON CONFLICT(id) DO UPDATE SET canonical_title=EXCLUDED.canonical_title, status=EXCLUDED.status,
              item_type=EXCLUDED.item_type,evidence_strength=EXCLUDED.evidence_strength,topics=EXCLUDED.topics
            """, new
        {
            item.Id,
            item.Slug,
            item.CanonicalTitle,
            ItemType = item.ItemType.ToString(),
            EvidenceStrength = item.EvidenceStrength.ToString(),
            Status = item.Status.ToString(),
            item.IsSynthetic,
            item.Topics,
            item.CreatedAt
        }, transaction, cancellationToken: cancellationToken));
        foreach (var source in item.Claims.SelectMany(x => x.Evidence).Select(x => x.SourceDocument).DistinctBy(x => x.Id))
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO source_documents(id,title,url,publisher,published_at,content_hash,snapshot_uri,is_synthetic)
                VALUES (@Id,@Title,@Url,@Publisher,@PublishedAt,@ContentHash,@SnapshotUri,@IsSynthetic) ON CONFLICT(id) DO NOTHING
                """, source, transaction, cancellationToken: cancellationToken));
            var unchanged = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
                SELECT EXISTS(SELECT 1 FROM source_documents WHERE id=@Id AND title=@Title AND url=@Url AND publisher=@Publisher
                  AND published_at=@PublishedAt AND content_hash=@ContentHash AND snapshot_uri=@SnapshotUri AND is_synthetic=@IsSynthetic)
                """, source, transaction, cancellationToken: cancellationToken));
            if (!unchanged) throw new InvalidOperationException("Source documents are append-only.");
        }
        foreach (var claim in item.Claims)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO claims(id,research_item_id,statement,claim_type,confidence) VALUES (@Id,@ResearchItemId,@Statement,@ClaimType,@Confidence)
                ON CONFLICT(id) DO UPDATE SET statement=EXCLUDED.statement,claim_type=EXCLUDED.claim_type,confidence=EXCLUDED.confidence
                WHERE claims.research_item_id=EXCLUDED.research_item_id
                """, new { claim.Id, claim.ResearchItemId, claim.Statement, ClaimType = claim.ClaimType.ToString(), Confidence = claim.Confidence.ToString() }, transaction, cancellationToken: cancellationToken));
            foreach (var entry in claim.Evidence)
                await connection.ExecuteAsync(new CommandDefinition("""
                    INSERT INTO evidence(id,claim_id,source_document_id,kind,excerpt,locator) VALUES (@Id,@ClaimId,@SourceDocumentId,@Kind,@Excerpt,@Locator)
                    ON CONFLICT(id) DO UPDATE SET kind=EXCLUDED.kind,excerpt=EXCLUDED.excerpt,locator=EXCLUDED.locator
                    WHERE evidence.claim_id=EXCLUDED.claim_id AND evidence.source_document_id=EXCLUDED.source_document_id
                    """, new { entry.Id, entry.ClaimId, entry.SourceDocumentId, Kind = entry.Kind.ToString(), entry.Excerpt, entry.Locator }, transaction, cancellationToken: cancellationToken));
        }
        foreach (var localization in item.Localizations)
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO localizations(id,research_item_id,locale,title,summary,why_it_matters,limitations,is_reviewed)
                VALUES (@Id,@ResearchItemId,@Locale,@Title,@Summary,@WhyItMatters,@Limitations,@IsReviewed)
                ON CONFLICT(id) DO UPDATE SET title=EXCLUDED.title,summary=EXCLUDED.summary,why_it_matters=EXCLUDED.why_it_matters,
                  limitations=EXCLUDED.limitations,is_reviewed=EXCLUDED.is_reviewed
                WHERE localizations.research_item_id=EXCLUDED.research_item_id AND localizations.locale=EXCLUDED.locale
                """, localization, transaction, cancellationToken: cancellationToken));
        var added = 0;
        foreach (var publication in item.PublicationVersions)
        {
            var inserted = await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO publication_versions(id,research_item_id,slug,locale,version,operation_id,published_at,change_note,snapshot_json)
                VALUES (@Id,@ResearchItemId,@Slug,@Locale,@Version,@OperationId,@PublishedAt,@ChangeNote,CAST(@SnapshotJson AS jsonb))
                ON CONFLICT(id) DO NOTHING
                """, publication, transaction, cancellationToken: cancellationToken));
            if (inserted == 0)
            {
                var unchanged = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
                    SELECT EXISTS(SELECT 1 FROM publication_versions WHERE id=@Id AND research_item_id=@ResearchItemId AND slug=@Slug
                      AND locale=@Locale AND version=@Version AND operation_id=@OperationId AND published_at=@PublishedAt
                      AND change_note=@ChangeNote AND snapshot_json=CAST(@SnapshotJson AS jsonb))
                    """, publication, transaction, cancellationToken: cancellationToken));
                if (!unchanged) throw new InvalidOperationException("Publication versions are append-only.");
                continue;
            }
            var message = new OutboxMessage(publication);
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO outbox_messages(id,aggregate_id,causation_id,correlation_id,event_type,schema_version,occurred_at,producer,payload_json,attempts)
                VALUES (@Id,@AggregateId,@CausationId,@CorrelationId,@EventType,@SchemaVersion,@OccurredAt,@Producer,CAST(@PayloadJson AS jsonb),@Attempts)
                """, message, transaction, cancellationToken: cancellationToken));
            added++;
        }
        return added;
    }

    private sealed class UtcTimestampHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value) => value is DateTimeOffset timestamp ? timestamp
            : new DateTimeOffset(DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc));
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value) => parameter.Value = value.UtcDateTime;
    }
}
