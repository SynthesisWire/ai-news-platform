# 04 — Research Ingestion Flow

```mermaid
sequenceDiagram
    participant S as Scheduler/Source
    participant I as Ingestion
    participant DB as PostgreSQL
    participant B as Blob Storage
    participant Q as Service Bus
    participant E as Enrichment
    participant M as Model Gateway

    S->>I: Source candidate / URL / feed entry
    I->>I: Normalize URL + source identity
    I->>DB: Check fingerprint/canonical duplicate
    alt New or materially changed
      I->>B: Store raw immutable snapshot
      I->>DB: Create SourceDocument/version
      I->>Q: SourceDocumentIngested
      Q->>E: Consume event
      E->>M: Extract entities/claims/evidence/concepts
      M-->>E: Structured response
      E->>E: Deterministic validation
      E->>DB: Upsert canonical knowledge objects
      E->>Q: ResearchItemCandidateReady
    else Duplicate / unchanged
      I->>DB: Update seen-at metadata only
    end
```

## Key rules

- Preserve immutable source snapshots for reproducibility.
- Deduplicate before expensive model inference.
- AI output never writes directly to public content.
- Claims and citations must reference source versions.
- Primary-source preference is encoded in source policy/scoring.
