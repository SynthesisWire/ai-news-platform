# AI Research Intelligence Platform

A scalable, multilingual AI research intelligence product: news, research items, trend tracking, evidence graphs, RSS/Atom, public web, and social distribution.

## Product thesis

The product is not a generic AI news blog. It is a public research-intelligence layer that separates **claims, evidence, limitations, sources, topics, trends, and relationships** and turns daily developments into a longitudinal knowledge base.

## Architecture principles

- PostgreSQL is the system of record; no NoSQL database is required.
- PostgreSQL Full-Text Search + pgvector power keyword and semantic retrieval initially.
- Raw source payloads and media live in object storage, not in the relational core.
- Start as a modular monolith with explicit domain/event boundaries.
- Use an Outbox pattern + Azure Service Bus for reliable asynchronous workflows.
- Keep AI providers behind a model gateway; no domain logic depends on one model vendor.
- Notion is an editorial adapter during the transition, not the public runtime database.
- Public content is versioned, evidence-backed, multilingual, SEO-ready, and correction-friendly.
- Website is canonical; RSS/Atom, X, newsletters, and future APIs are projections of the same publication model.

## Proposed stack

| Layer | Technology |
|---|---|
| Public Web | Next.js / React / TypeScript |
| Backend/API | .NET 10 / ASP.NET Core |
| Persistence | PostgreSQL + pgvector + FTS |
| ORM | EF Core |
| Async Messaging | Azure Service Bus |
| Reliable Events | Transactional Outbox in PostgreSQL |
| Background Processing | .NET Worker Services / Quartz.NET for scheduled orchestration |
| Raw Documents / Media | Azure Blob Storage |
| Runtime | Azure Container Apps |
| Edge / CDN / WAF | Azure Front Door |
| Observability | OpenTelemetry + Application Insights / compatible backend |
| CI/CD | GitHub Actions |
| Infrastructure | Terraform |

## Repository map

```text
/src/frontend       Public website + future editorial UI
/src/backend        ASP.NET Core API + domain/application modules
/src/workers        ingestion, enrichment, translation, publishing workers
/docs/product       product definition and publishing model
/docs/architecture  C4/flow/data diagrams
/docs/adr           architectural decisions
/infra              Terraform / deployment assets
/tests               integration, contract, architecture and E2E tests
```

Start with [Product Architecture](docs/architecture/01-system-context.md) and [MVP Roadmap](docs/product/roadmap.md).
