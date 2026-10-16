# SynthesisWire

**Editorial Intelligence**

An evidence-first, bilingual research intelligence product. The canonical record separates claims, evidence, limitations, source versions, and publication history.

## Executable foundation

This repository now contains a .NET 10 API, EF Core/PostgreSQL schema, and a Next.js/React/TypeScript website in `en` and `pt-BR`.

The development dataset is **entirely synthetic**. It is labeled in the API and UI, cites reserved `example.org` URLs, and must not be presented as real research or news. No private Notion content, external AI provider, or cloud account is required.

### Start locally with Docker

Prerequisite: Docker with Compose v2.

```sh
cp .env.example .env
docker compose up --build
```

Compose waits for PostgreSQL, applies versioned migrations, seeds the demo idempotently, then starts the API and website. All published ports bind to loopback.

- Website: <http://localhost:3000/pt-BR> or <http://localhost:3000/en>
- API: <http://localhost:5080/api/research-items?locale=en>
- Liveness: <http://localhost:5080/health/live>
- Database readiness: <http://localhost:5080/health/ready>

```sh
node scripts/smoke.mjs
```

Stop with `docker compose down`. Data persists in a named volume. Use `docker compose down --volumes` only when you intend to delete the local development database.

### Develop without rebuilding containers

Install the .NET 10 SDK and Node.js 24 LTS, then start only PostgreSQL:

```sh
docker compose up -d postgres
export ConnectionStrings__Postgres='Host=localhost;Port=5432;Database=airesearch;Username=airesearch;Password=local-development-only'
dotnet restore
dotnet run --project src/backend/AiResearch.Api -- --migrate --seed-demo
dotnet run --project src/backend/AiResearch.Api -- --urls http://localhost:5080
```

Use the password selected in `.env` when constructing the connection string. The API does not load `.env` automatically, and normal API startup never applies migrations or adds demo content.

In a second terminal:

```sh
cd src/frontend/apps/web
npm ci
npm run dev
```

The website fetches the API server-side using `API_BASE_URL` (default `http://localhost:5080`). Set `SITE_URL` to the public web origin when testing canonical metadata. There is no fixture fallback when the API is unavailable.

See [development and verification](docs/development.md) for tests, schema changes, and delivery boundaries.

## Architecture

- PostgreSQL is the canonical store; immutable publication snapshots power public reads.
- Claims link to evidence and immutable source-document versions.
- Locale renderings share canonical IDs and evidence rather than creating separate research records.
- A modular monolith keeps domain rules independent of web and persistence concerns.
- Publication writes and outbox records share a transaction. External delivery is a later increment.
- Notion is an editorial adapter, never the public runtime database.
- The website is canonical; feeds and future distribution are projections of published versions.

The complete product architecture remains the target. This first executable foundation does **not** claim to implement the complete [URL-to-publication slice](docs/architecture/12-first-end-to-end-slice.md).

## Repository map

```text
src/backend       .NET domain, application, infrastructure, and read API
src/frontend/apps/web
                  Bilingual Next.js public website
scripts           Live-stack smoke checks
tests             Backend domain, architecture, and PostgreSQL integration tests
.github/workflows Automated verification
/docs/product     Product definition, roadmap, and backlog
/docs/architecture
                  Architecture and flow diagrams
/docs/adr         Architecture decisions
```

Start with [Product Architecture](docs/architecture/01-system-context.md), [MVP Roadmap](docs/product/roadmap.md), and [delivery boundaries](docs/development.md#delivery-boundaries).
