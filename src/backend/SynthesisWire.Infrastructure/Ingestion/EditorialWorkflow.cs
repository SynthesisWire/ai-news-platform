using System.Text.Json;
using SynthesisWire.Domain;
using SynthesisWire.Infrastructure.Persistence;
using Dapper;
using Npgsql;

namespace SynthesisWire.Infrastructure.Ingestion;

public sealed record HumanLocale(string Title, string Summary, string WhyItMatters, string[] Limitations);
public sealed record HumanClaim(string Statement, string Excerpt, string Locator);
public sealed record HumanDraft(string Slug, string CanonicalTitle, string SourceTitle, string Publisher,
    DateTimeOffset SourcePublishedAt, bool RightsConfirmed, HumanClaim[] Claims, HumanLocale English, HumanLocale Portuguese);
public sealed record EditorialRequest(string Action, string? Url = null, Guid DraftId = default,
    int ExpectedRevision = 0, HumanDraft? Content = null, bool Confirmed = false);
public sealed record DraftResult(Guid Id, Guid CaptureId, Guid ResearchItemId, int Revision, string State, Guid? PossibleDuplicateCaptureId = null);

// Administrative CLI only. The database account and OS process owner are the local trust boundary.
// No registration, persistent application credentials or unauthenticated HTTP write routes are created.
public sealed class EditorialWorkflow(NpgsqlDataSource dataSource, ResearchStore store, ISourceFetcher fetcher)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow };

    public async Task<DraftResult> IngestAsync(string url, string actor, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        var capture = await fetcher.FetchAsync(url, cancellationToken);
        if (capture.Body.Length is < 1 or > SafeSourceFetcher.MaximumBytes) throw new InvalidOperationException("Invalid capture size.");
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(hashtextextended(@Url,1))", new { Url = capture.RequestedUrl }, transaction, cancellationToken: cancellationToken));
        var captureId = await connection.QuerySingleOrDefaultAsync<Guid?>(new CommandDefinition("""
            SELECT id FROM source_captures WHERE requested_url=@RequestedUrl AND content_hash=@ContentHash
            """, new { capture.RequestedUrl, capture.ContentHash }, transaction, cancellationToken: cancellationToken));
        if (captureId is null)
        {
            captureId = Guid.CreateVersion7();
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO source_captures(id,requested_url,final_url,media_type,body,content_hash,material_hash,fetched_at,is_synthetic)
                VALUES (@Id,@RequestedUrl,@FinalUrl,@MediaType,@Body,@ContentHash,@MaterialHash,@FetchedAt,@IsSynthetic)
                """, new { Id = captureId, capture.RequestedUrl, capture.FinalUrl, capture.MediaType, capture.Body, capture.ContentHash, capture.MaterialHash, capture.FetchedAt, capture.IsSynthetic }, transaction, cancellationToken: cancellationToken));
            var draftId = Guid.CreateVersion7();
            await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO editorial_drafts(id,capture_id,author,research_item_id,en_operation_id,pt_operation_id)
                VALUES (@Id,@CaptureId,@Actor,@ResearchId,@EnglishId,@PortugueseId)
                """, new { Id = draftId, CaptureId = captureId, Actor = actor, ResearchId = Guid.CreateVersion7(), EnglishId = Guid.CreateVersion7(), PortugueseId = Guid.CreateVersion7() }, transaction, cancellationToken: cancellationToken));
            await AuditAsync(connection, transaction, draftId, 0, "Ingest", actor, cancellationToken);
        }
        var row = await LoadAsync(connection, transaction, captureId.Value, byCapture: true, cancellationToken);
        var duplicate = await connection.QueryFirstOrDefaultAsync<Guid?>(new CommandDefinition("""
            SELECT id FROM source_captures WHERE material_hash=@MaterialHash AND id<>@Id ORDER BY fetched_at,id LIMIT 1
            """, new { capture.MaterialHash, Id = captureId }, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return Result(row) with { PossibleDuplicateCaptureId = duplicate };
    }

    public async Task<object> ExecuteAsync(EditorialRequest request, string actor, CancellationToken cancellationToken = default)
    {
        ValidateActor(actor);
        if (request.Action == "ingest") return await IngestAsync(request.Url ?? throw new InvalidOperationException("URL is required."), actor, cancellationToken);
        if (request.DraftId == Guid.Empty) throw new InvalidOperationException("Draft ID is required.");
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var row = await LoadAsync(connection, transaction, request.DraftId, byCapture: false, cancellationToken);
        var capture = await connection.QuerySingleAsync<CaptureRow>(new CommandDefinition("""
            SELECT id, requested_url AS "RequestedUrl", final_url AS "FinalUrl", media_type AS "MediaType", body,
              content_hash AS "ContentHash", fetched_at AS "FetchedAt", is_synthetic AS "IsSynthetic"
            FROM source_captures WHERE id=@Id
            """, new { Id = row.CaptureId }, transaction, cancellationToken: cancellationToken));
        if (request.Action == "show")
            return new
            {
                Draft = Result(row),
                Content = row.PayloadJson is null ? null : JsonSerializer.Deserialize<HumanDraft>(row.PayloadJson, Json),
                capture.RequestedUrl,
                capture.FinalUrl,
                capture.ContentHash,
                capture.FetchedAt,
                capture.MediaType,
                SourceText = new System.Text.UTF8Encoding(false, true).GetString(capture.Body)
            };
        if (request.ExpectedRevision != row.Revision) throw new InvalidOperationException("Stale draft revision; inspect the current draft before retrying.");
        if (row.State == "Published" && request.Action == "publish") return Result(row); // Stable publication operation IDs.
        var content = request.Action == "save" ? request.Content : row.PayloadJson is null ? null : JsonSerializer.Deserialize<HumanDraft>(row.PayloadJson, Json);
        if (content is null) throw new InvalidOperationException("A complete human-authored draft is required.");
        ValidateContent(content, capture);
        string state;
        var revision = row.Revision;
        switch (request.Action)
        {
            case "save" when row.State == "Draft":
                state = "Draft";
                revision++;
                break;
            case "submit" when row.State == "Draft":
                state = "InReview";
                break;
            case "revise" when row.State is "InReview" or "Reviewed":
                state = "Draft"; // Returning to draft invalidates human approval.
                revision++;
                break;
            case "review" when row.State == "InReview" && request.Confirmed:
                state = "Reviewed";
                break;
            case "publish" when row.State == "Reviewed":
                state = "Published";
                var item = BuildItem(row, capture, content);
                await store.SaveAsync(item, connection, transaction, cancellationToken);
                break;
            default:
                throw new InvalidOperationException("Invalid editorial transition or missing explicit review confirmation.");
        }
        await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE editorial_drafts SET state=@State,revision=@Revision,payload_json=CAST(@Payload AS jsonb),
              reviewer=CASE WHEN @State='Reviewed' THEN @Actor WHEN @State='Draft' THEN NULL ELSE reviewer END WHERE id=@Id
            """, new { State = state, Revision = revision, Payload = JsonSerializer.Serialize(content, Json), Actor = actor, row.Id }, transaction, cancellationToken: cancellationToken));
        await AuditAsync(connection, transaction, row.Id, revision, request.Action, actor, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new DraftResult(row.Id, row.CaptureId, row.ResearchItemId, revision, state);
    }

    private static ResearchItem BuildItem(DraftRow row, CaptureRow capture, HumanDraft draft)
    {
        var source = new SourceDocument(capture.Id, draft.SourceTitle, capture.FinalUrl, draft.Publisher,
            draft.SourcePublishedAt, capture.ContentHash, $"capture://{capture.Id}", capture.IsSynthetic);
        var item = new ResearchItem(row.ResearchItemId, draft.Slug, draft.CanonicalTitle, ResearchItemType.ResearchNote,
            capture.IsSynthetic ? EvidenceStrength.Synthetic : EvidenceStrength.Limited, capture.IsSynthetic, [], DateTimeOffset.UtcNow);
        foreach (var claim in draft.Claims)
            item.AddClaim(claim.Statement, capture.IsSynthetic ? ClaimType.SyntheticIllustration : ClaimType.AuthorClaim, Confidence.Low,
                new Evidence(source, capture.IsSynthetic ? EvidenceKind.SyntheticFixture : EvidenceKind.Paper, claim.Excerpt, claim.Locator));
        item.SetLocalization("en", draft.English.Title, draft.English.Summary, draft.English.WhyItMatters, draft.English.Limitations);
        item.SetLocalization("pt-BR", draft.Portuguese.Title, draft.Portuguese.Summary, draft.Portuguese.WhyItMatters, draft.Portuguese.Limitations);
        item.SubmitForReview();
        item.ReviewLocalization("en");
        item.ReviewLocalization("pt-BR");
        item.Approve();
        var publishedAt = DateTimeOffset.UtcNow;
        item.Publish("en", row.EnOperationId, publishedAt, "Human-reviewed initial publication.");
        item.Publish("pt-BR", row.PtOperationId, publishedAt, "Publicação inicial revisada por pessoa responsável.");
        return item;
    }

    private static void ValidateContent(HumanDraft content, CaptureRow capture)
    {
        Text(content.Slug, 180);
        Text(content.CanonicalTitle, 500);
        Text(content.SourceTitle, 500);
        Text(content.Publisher, 300);
        if (!content.RightsConfirmed || content.SourcePublishedAt == default || content.SourcePublishedAt > DateTimeOffset.UtcNow
            || content.Claims is null || content.Claims.Length is < 1 or > 8)
            throw new InvalidOperationException("Confirm reuse rights, source date and one to eight attributed claims.");
        var source = new System.Text.UTF8Encoding(false, true).GetString(capture.Body);
        foreach (var claim in content.Claims)
        {
            if (claim is null) throw new InvalidOperationException("Every claim must be structured editorial data.");
            Text(claim.Statement, 1000);
            Text(claim.Excerpt, 280);
            Text(claim.Locator, 300);
            if (!source.Contains(claim.Excerpt, StringComparison.Ordinal)) throw new InvalidOperationException("Evidence excerpt must occur verbatim in the captured source.");
        }
        if (content.Claims.Sum(x => x.Excerpt.Length) > 560) throw new InvalidOperationException("Total published source excerpts exceed this increment's reuse limit.");
        foreach (var locale in new[] { content.English, content.Portuguese })
        {
            if (locale is null) throw new InvalidOperationException("Human-authored English and Portuguese text is required.");
            Text(locale.Title, 500); Text(locale.Summary, 1500); Text(locale.WhyItMatters, 1500);
            if (locale.Limitations is null || locale.Limitations.Length is < 1 or > 8) throw new InvalidOperationException("Document limitations in both languages.");
            foreach (var limitation in locale.Limitations) Text(limitation, 500);
        }
        // Validate slug and source metadata with the existing domain guards without publishing anything.
        _ = new ResearchItem(Guid.CreateVersion7(), content.Slug, content.CanonicalTitle, ResearchItemType.ResearchNote, EvidenceStrength.Limited, false, [], DateTimeOffset.UtcNow);
    }

    private static void Text(string? value, int maximum)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maximum || value.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
            throw new InvalidOperationException("Editorial text is missing, too long, or contains control characters.");
    }
    private static void ValidateActor(string actor) => Text(actor, 200);
    private static DraftResult Result(DraftRow row) => new(row.Id, row.CaptureId, row.ResearchItemId, row.Revision, row.State);
    private static async Task<DraftRow> LoadAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid id, bool byCapture, CancellationToken token)
        => await connection.QuerySingleOrDefaultAsync<DraftRow>(new CommandDefinition("""
            SELECT id,capture_id AS "CaptureId",research_item_id AS "ResearchItemId",revision,state,payload_json::text AS "PayloadJson",
              en_operation_id AS "EnOperationId",pt_operation_id AS "PtOperationId" FROM editorial_drafts
            """ + (byCapture ? " WHERE capture_id=@Id FOR UPDATE" : " WHERE id=@Id FOR UPDATE"), new { Id = id }, transaction, cancellationToken: token))
            ?? throw new InvalidOperationException("Draft not found.");
    private static async Task AuditAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, Guid id, int revision, string action, string actor, CancellationToken token)
        => await connection.ExecuteAsync(new CommandDefinition("INSERT INTO editorial_events(id,draft_id,revision,action,actor) VALUES (@EventId,@Id,@Revision,@Action,@Actor)",
            new { EventId = Guid.CreateVersion7(), Id = id, Revision = revision, Action = action, Actor = actor }, transaction, cancellationToken: token));
    private sealed class DraftRow
    {
        public Guid Id { get; set; }
        public Guid CaptureId { get; set; }
        public Guid ResearchItemId { get; set; }
        public int Revision { get; set; }
        public string State { get; set; } = "";
        public string? PayloadJson { get; set; }
        public Guid EnOperationId { get; set; }
        public Guid PtOperationId { get; set; }
    }
    private sealed class CaptureRow
    {
        public Guid Id { get; set; }
        public string RequestedUrl { get; set; } = "";
        public string FinalUrl { get; set; } = "";
        public string MediaType { get; set; } = "";
        public byte[] Body { get; set; } = [];
        public string ContentHash { get; set; } = "";
        public DateTimeOffset FetchedAt { get; set; }
        public bool IsSynthetic { get; set; }
    }
}
