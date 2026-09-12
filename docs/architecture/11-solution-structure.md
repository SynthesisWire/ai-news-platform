# 11 — Suggested Solution Structure

```text
src/
  backend/
    AiResearch.Api/
    AiResearch.Domain/
    AiResearch.Application/
    AiResearch.Infrastructure/
    Modules/
      Sources/
      Ingestion/
      Knowledge/
      Editorial/
      Trends/
      Localization/
      Publishing/
      Distribution/
      Search/
  workers/
    AiResearch.Worker.Ingestion/
    AiResearch.Worker.Enrichment/
    AiResearch.Worker.Publishing/
    AiResearch.Worker.Distribution/
  frontend/
    apps/web/
    packages/ui/
    packages/api-client/
tests/
  ArchitectureTests/
  IntegrationTests/
  ContractTests/
  E2E/
infra/
  terraform/
  github-actions/
```

## Backend layering

Keep Clean Architecture pragmatically inside each module: domain rules inward, infrastructure outward. Prefer vertical slices for use cases instead of one global Commands/Queries folder.
