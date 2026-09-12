# 10 — Deployment Topology

```mermaid
flowchart TB
  Users --> FD[Azure Front Door]
  FD --> WEB[Container App: Web]
  FD --> API[Container App: API]
  API --> PG[(Azure PostgreSQL Flexible Server)]
  API --> BLOB[(Azure Blob Storage)]
  API --> SB[Azure Service Bus]
  SB --> W1[Worker: Ingestion]
  SB --> W2[Worker: Enrichment]
  SB --> W3[Worker: Publication]
  SB --> W4[Worker: Distribution]
  W1 --> PG
  W2 --> PG
  W3 --> PG
  W4 --> PG
  W1 --> BLOB
  W2 --> AI[External / Local AI Providers]
```

## Scaling model

- Web/API scale horizontally on HTTP concurrency.
- Workers scale independently on Service Bus queue depth.
- PostgreSQL read replicas can be introduced for public read/search workloads.
- Blob/CDN handles immutable source/media distribution.
- Heavy enrichment is decoupled from publishing and public traffic.
