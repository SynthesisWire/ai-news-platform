# 07 — Core Relational Data Model

```mermaid
erDiagram
    RESEARCH_ITEM ||--o{ CLAIM : contains
    CLAIM ||--o{ EVIDENCE : supported_by
    EVIDENCE }o--|| SOURCE_DOCUMENT : cites
    SOURCE ||--o{ SOURCE_DOCUMENT : versions
    RESEARCH_ITEM }o--o{ TOPIC : classified_as
    RESEARCH_ITEM }o--o{ ENTITY : mentions
    RESEARCH_ITEM ||--o{ LOCALIZATION : rendered_as
    RESEARCH_ITEM ||--o{ PUBLICATION_VERSION : publishes
    RESEARCH_ITEM ||--o{ RESEARCH_RELATION : from_item
    RESEARCH_ITEM ||--o{ RESEARCH_RELATION : to_item
    TOPIC ||--o{ TREND_SNAPSHOT : has
    DAILY_EDITION }o--o{ RESEARCH_ITEM : includes
    RESEARCH_NOTE }o--o{ RESEARCH_ITEM : correlates
    CONCEPT }o--o{ RESEARCH_ITEM : explains

    RESEARCH_ITEM {
      uuid id PK
      text canonical_title
      text item_type
      text status
      int importance
      text evidence_strength
      timestamptz published_at
      timestamptz created_at
    }
    CLAIM {
      uuid id PK
      uuid research_item_id FK
      text statement
      text claim_type
      text confidence
    }
    EVIDENCE {
      uuid id PK
      uuid claim_id FK
      uuid source_document_id FK
      text evidence_kind
      text excerpt_locator
    }
    SOURCE_DOCUMENT {
      uuid id PK
      uuid source_id FK
      text canonical_url
      text content_hash
      timestamptz source_published_at
      text blob_snapshot_uri
    }
```

## Search

- `tsvector` columns for FTS.
- `vector` columns via pgvector for semantic retrieval.
- Reciprocal-rank-fusion or weighted hybrid ranking at query time.
- Promote source/evidence/trend filters in SQL rather than a separate search datastore initially.
