# Initial Product Backlog

## Epic A — Platform Foundation

1. Bootstrap .NET solution and Next.js workspace.
2. Add PostgreSQL + pgvector migrations.
3. Implement Outbox + idempotent consumer primitives.
4. Add Azure Service Bus abstraction and local dev fallback.
5. Add OpenTelemetry baseline and correlation IDs.
6. Add architecture tests enforcing module boundaries.

## Epic B — Source & Ingestion

1. Source registry and source trust policy.
2. Manual URL ingestion endpoint.
3. HTTP fetcher with canonical URL normalization.
4. Immutable snapshot persistence in object storage.
5. Exact/hash duplicate detection.
6. Material-change detection between source versions.
7. RSS/Atom connector.
8. arXiv / Crossref / company research connector strategy.
9. Notion AI Research Hub import adapter.

## Epic C — Knowledge Core

1. ResearchItem aggregate.
2. Claim + Evidence model.
3. Source / SourceDocument versioning.
4. Entity / Concept / Topic taxonomy.
5. Typed ResearchRelation model.
6. DailyEdition + ResearchNote model.
7. Evidence-strength and importance scoring.
8. Correction/version history.

## Epic D — AI Enrichment

1. Provider-neutral ModelGateway.
2. Structured-output contracts.
3. Claim/evidence extraction.
4. Entity/concept extraction.
5. Topic classification.
6. Relationship proposals.
7. Trend evidence proposals.
8. Deterministic validation and confidence thresholds.
9. Provider cost/latency telemetry.

## Epic E — Editorial

1. Draft/review/approve/reject/correct workflow.
2. Editorial UI adapter API.
3. Notion sync for candidates and review status.
4. Evidence review screen.
5. Publication preview.
6. Audit history.

## Epic F — Localization

1. Canonical semantic record.
2. PT-BR and English rendering pipeline.
3. Immutable evidence/citation fields across locales.
4. Locale review status.
5. hreflang and canonical metadata.

## Epic G — Public Web

1. Home / Daily Brief.
2. Research Item page.
3. Topic/Trend page.
4. Concept page.
5. Daily archive.
6. Corrections/version history UI.
7. SEO + JSON-LD + sitemap.
8. OpenGraph cards.

## Epic H — Search & Discovery

1. PostgreSQL FTS index.
2. pgvector embeddings.
3. Hybrid ranking.
4. Filters: source, date, type, evidence, topic, organization.
5. Related research.
6. Recursive relationship traversal API.
7. Trend timeline query.

## Epic I — Syndication & Distribution

1. Global RSS feed.
2. Locale RSS feeds.
3. Topic RSS feeds.
4. Atom/JSON Feed optional projection.
5. X draft generator.
6. X approval/publish integration.
7. Newsletter projection.
8. Distribution audit log.

## Epic J — Reliability & Security

1. Idempotency across all workers.
2. Dead-letter/retry policies.
3. Source fetch rate limits.
4. SSRF/content-fetch protections.
5. Prompt-injection isolation for ingested content.
6. Publication authorization boundary.
7. Secrets via managed identity / Key Vault.
8. Backup/restore and correction recovery drills.
