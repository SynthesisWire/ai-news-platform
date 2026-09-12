# 08 — Event Catalog

Initial integration events:

- `SourceDocumentIngested`
- `SourceDocumentChanged`
- `ResearchItemCandidateReady`
- `ResearchItemEnriched`
- `ResearchItemApproved`
- `PublicationScheduled`
- `ContentPublished`
- `ContentCorrected`
- `LocalizationRequested`
- `LocalizationCompleted`
- `TrendEvidenceChanged`
- `DistributionRequested`
- `DistributionCompleted`

All events use an envelope with event id, aggregate id, causation id, correlation id, schema version, occurred-at and producer.

Reliable publishing uses the Transactional Outbox pattern in PostgreSQL.
