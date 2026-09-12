# 09 — Search Architecture

## Phase 1: PostgreSQL only

```mermaid
flowchart LR
 Q[User Query] --> P[Query Parser]
 P --> FTS[PostgreSQL Full Text]
 P --> V[pgvector ANN]
 FTS --> R[Hybrid Ranker]
 V --> R
 R --> Filters[Evidence / Topic / Date / Type / Language filters]
 Filters --> Results[Ranked Results]
```

Use PostgreSQL for keyword, semantic, filters, facets, and related-content generation until measured load demonstrates a separate search engine is necessary.

## Later extraction trigger

Consider OpenSearch/Elasticsearch only if measurements show PostgreSQL cannot meet indexing/query latency, relevance experimentation, or operational isolation requirements. This is an explicit future decision, not an MVP dependency.
