# Development and verification

## Local services

`docker compose up --build` starts PostgreSQL 17 with pgvector, a one-shot migration/demo-seed job, the public API, and the website. The demo job is explicitly invoked only by this local-development Compose file. API startup itself never changes the schema or seeds data.

Supply a unique development database secret via `POSTGRES_PASSWORD` in the environment or ignored `.env`; no committed default is provided. See the README for a runtime generation command. Keep the same secret for the lifetime of the local database volume. Compose binds ports to `127.0.0.1`; do not use this file as an internet-facing deployment. There are no credentials for Azure, AI providers, Notion, or any other external service.

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
export TEST_POSTGRES_CONNECTION_STRING="Host=localhost;Port=5432;Database=airesearch;Username=airesearch;Password=${POSTGRES_PASSWORD:?Supply your disposable database secret}"
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

## Credential policy

All passwords, including development and test credentials, must be supplied at runtime. `.env.example` deliberately leaves the password empty; actual `.env` files remain ignored. The API and migration command require an explicit `ConnectionStrings__Postgres` and have no fallback. Avoid printing environment files, connection strings, or resolved Compose configuration.

CI generates a fresh random credential in each job, masks it, and passes it through the job environment to disposable PostgreSQL and tests. It does not store generated secrets in repository files or artifacts. Run `node --test scripts/credential-policy.test.mjs` and `node scripts/check-credentials.mjs` to check the tracked tree for password assignments, credential-bearing URLs, and accidentally tracked environment files. The check reports paths and line numbers, never candidate values. It is a regression guard, not a guarantee that every possible secret format can be recognized.

## SQL persistence and compatibility

`SynthesisWire.Infrastructure` uses Dapper with Npgsql and explicit parameterized SQL. Domain and application projects have no persistence dependencies. Public reads use only immutable publication JSON, keeping draft edits private. Publications and outbox envelopes are inserted in the same transaction; a repeated publication operation does not enqueue another event. Explicit demo initialization uses a transaction advisory lock for concurrent idempotency.

Run `dotnet run --project src/backend/SynthesisWire.Api -- --migrate` with an externally supplied `ConnectionStrings__Postgres`. Versioned SQL under `Persistence/SqlMigrations` is embedded in the infrastructure assembly. The runner serializes migration jobs, applies all pending SQL and journal entries in one transaction, and records SHA-256 checksums in `schema_migrations`. It rejects changed or unknown migration history and checks a fingerprint of application columns, constraints, indexes, immutable triggers/functions and the vector extension. Readiness checks the same metadata without applying changes. Do not edit applied migrations; add a new numbered SQL file. This is a local foundation migration mechanism, not a production deployment workflow.

Databases created by the earlier EF implementation or an untracked schema are rejected without overwriting data. Keep those databases intact. Moving an existing database requires a separately reviewed, data-preserving schema comparison and baseline; no automatic conversion, destructive reset, or ID rewrite is performed. For a new disposable environment, initialize a fresh database. Existing lower-case database/role names and Compose volume naming remain compatible with local configuration; all .NET projects, namespaces and assemblies use `SynthesisWire`.

New generated domain, publication and operation identifiers use `Guid.CreateVersion7()`; C# `Guid` and PostgreSQL `uuid` remain the storage types. Outbox IDs reuse the publication UUID v7 for atomic retry identity. The fixed synthetic research/source/seed-operation IDs remain unchanged to preserve fixture identity and repeated seed behavior. Existing persisted IDs, including older UUID versions, are accepted unchanged. Tests verify UUID version and uniqueness without depending on clock ordering.

## Editor, formatting and lint checks

The root `.editorconfig` defines UTF-8/LF, C# indentation and common .NET style, with separate two-space frontend/XML rules. The build runs the .NET SDK analyzers with warnings treated as errors. Run `dotnet format whitespace --no-restore` to format C#, or add `--verify-no-changes` to check it.

From `src/frontend/apps/web`, use `npm run lint`, `npm run format`, `npm run format:check`, or `npm run check` for the complete lint/format/typecheck/test/build sequence. ESLint keeps the Next core-web-vitals, TypeScript and React hooks recommendations. Prettier owns formatting, and eslint-config-prettier disables conflicting formatting rules. Generated output and dependencies are ignored. TypeScript 6.0.3 is used because the Next ESLint parser does not support the TypeScript 7 API. CI runs both frontend lint/format checks and .NET whitespace/analyzer checks.

The Next 16.4.2 ESLint dependency chain currently brings an upstream braces denial-of-service advisory through fast-glob/micromatch, with no compatible patched braces release available at verification time. These are development-only lint dependencies; production dependencies are checked separately. Keep lint inputs limited to the repository and update the Next ESLint package when a compatible fix is released. Do not remove recommended Next checks or apply an incompatible major downgrade merely to suppress the audit result.
