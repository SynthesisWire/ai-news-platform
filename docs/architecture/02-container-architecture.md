# 02 — Container Architecture

```mermaid
flowchart TB
    subgraph Edge
      FD[Azure Front Door\nCDN + WAF]
    end

    subgraph Apps[Azure Container Apps]
      WEB[Next.js Public Web]
      API[ASP.NET Core API\nModular Monolith]
      ING[Ingestion Worker]
      ENR[Enrichment Worker]
      PUB[Publishing Worker]
      DIST[Distribution Worker]
      SCH[Scheduler / Quartz]
    end

    subgraph Data
      PG[(PostgreSQL\nRelational + FTS + pgvector)]
      BLOB[(Blob Storage\nraw snapshots + assets)]
      SB[Azure Service Bus]
    end

    subgraph External
      SRC[External Sources]
      NOTION[Notion]
      MODELS[AI Providers\nOpenAI · Anthropic · Google · Local]
      SOCIAL[X / Email / other channels]
    end

    FD --> WEB
    WEB --> API
    API --> PG
    API --> BLOB
    API --> SB
    SRC --> ING
    NOTION <--> ING
    ING --> PG
    ING --> BLOB
    ING --> SB
    SB --> ENR
    ENR --> MODELS
    ENR --> PG
    ENR --> SB
    SB --> PUB
    PUB --> PG
    PUB --> SB
    SB --> DIST
    DIST --> SOCIAL
    SCH --> SB
```

## Why modular monolith first

The API is one deployable initially, but organized into independent modules. Workers can scale independently from day one. High-throughput modules can later be extracted behind the same event contracts.
