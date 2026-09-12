# 13 — Relationship Graph Without a Graph Database

The research graph remains relational in PostgreSQL.

```mermaid
erDiagram
  RESEARCH_ITEM ||--o{ RESEARCH_RELATION : source
  RESEARCH_ITEM ||--o{ RESEARCH_RELATION : target
  RESEARCH_RELATION {
    uuid id PK
    uuid source_item_id FK
    uuid target_item_id FK
    text relation_type
    text confidence
    text rationale
    uuid evidence_id FK
    timestamptz valid_from
    timestamptz valid_to
  }
```

Supported relation types initially:

- supports
- contradicts
- extends
- depends_on
- alternative_to
- evidence_for
- evidence_against
- caused_by
- supersedes

## Query strategy

- direct relationships: indexed joins;
- bounded graph exploration: recursive CTEs;
- popular topic graphs: materialized projections/views;
- semantic discovery: pgvector candidates followed by relational validation;
- public visualization payloads: precomputed JSON projections generated from relational state.

A dedicated graph database is deferred until measured query patterns prove PostgreSQL insufficient.
