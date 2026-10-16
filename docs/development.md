# Development and verification

## Local services

`docker compose up --build` starts PostgreSQL 17 with pgvector, a one-shot migration/demo-seed job, the public API, and the website. The demo job is explicitly invoked only by this local-development Compose file. API startup itself never changes the schema or seeds data.

The fixed development password is not a production secret. Compose binds ports to `127.0.0.1`; do not use this file as an internet-facing deployment. There are no credentials for Azure, AI providers, Notion, or any other external service.

Database readiness and application liveness are separate:

- `GET /health/live`: the API process is serving requests.
- `GET /health/ready`: the canonical PostgreSQL dependency is reachable.

Public read endpoints:

- `GET /api/research-items?locale=en`
- `GET /api/research-items/{slug}?locale=pt-BR`

Supported locales are exactly `en` and `pt-BR`. Unsupported locales return `400`; missing publications return `404`. Only published snapshots can appear in public reads. There are no editorial write routes.

The website provides `/en`, `/pt-BR`, `/[locale]/research/[slug]`, `/[locale]/rss.xml`, and `/sitemap.xml`. `API_BASE_URL` is a server-only origin; `SITE_URL` is the canonical web origin. API outages surface an unavailable state rather than fabricated articles.

## Verification

From the repository root:

```sh
dotnet restore
dotnet build --no-restore --configuration Release
dotnet test --no-build --configuration Release
```

The PostgreSQL integration tests require an explicit connection string and skip when it is absent. A unit-only pass is not database verification. Use a disposable development/test PostgreSQL server whose account has `CREATEDB` permission and can install the vector extension:

```sh
export TEST_POSTGRES_CONNECTION_STRING='Host=localhost;Port=5432;Database=airesearch;Username=airesearch;Password=local-development-only'
dotnet test --configuration Release
```

Each integration test creates a uniquely named test database and drops only that database afterward. The supplied database is used only as the administrative connection; its application tables are never modified. Do not point this suite at production.

From `src/frontend/apps/web`:

```sh
npm ci
npm run typecheck
npm test
npm run build
npx playwright install chromium
E2E_BASE_URL=http://127.0.0.1:3000 npm run test:e2e
# Separate test-only API fixture exercises outages and retry recovery:
npm run test:e2e
```

The live-stack smoke check requires the running seeded API and website:

```sh
node scripts/smoke.mjs
```

Override `API_BASE_URL` and `WEB_BASE_URL` for non-default ports. The smoke test checks health, canonical identity across locales, evidence provenance, version history, rendered article pages, validation, and missing content.

The GitHub Actions workflow is configured to verify the container build/start path as well. Check its run results before treating that path as verified. No workflow deploys or merges changes.

## Delivery boundaries

Implemented in this foundation:

- .NET 10 solution with inward domain/application dependencies and an outward persistence/API layer
- Relational canonical research, sources, claims, evidence, locale renderings, and publication versions
- Versioned PostgreSQL migrations, pgvector extension, and full-text/vector schema groundwork
- Publication invariants and immutable public snapshots; transactional outbox storage
- Explicit, repeatable synthetic data initialization and a read-only public API
- Bilingual responsive website with evidence/source detail, limitations, revision history, canonical metadata, sitemap, and RSS
- Domain, architecture, contract, integration, frontend, and browser verification

Not implemented yet:

- Manual URL fetching, SSRF-hardened ingestion, raw object-storage snapshots, or material-change deduplication
- Live AI extraction/translation or a provider gateway
- Authenticated editorial UI/API, review permissions, or Notion import/sync
- Outbox relay, Azure Service Bus, consumers, retries, or dead-letter processing
- Keyword/semantic search endpoints or generated embeddings
- Live news, daily briefs, trend/relationship views, or social/newsletter delivery
- Production infrastructure, secrets, backups, telemetry export, moderation policies, or deployment

Source documents in the fixture represent canonical provenance, not fetched source snapshots. Reserved `example.org` links deliberately do not establish real evidence. The outbox is persisted groundwork; it does not imply successful event delivery.

The [first end-to-end slice](architecture/12-first-end-to-end-slice.md) and [roadmap](product/roadmap.md) remain open beyond this increment. Add real ingestion and a human-gated editorial path next, with explicit authority and SSRF/content isolation before enabling any external fetches or publication.
