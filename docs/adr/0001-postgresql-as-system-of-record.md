# ADR-0001 — PostgreSQL as the System of Record

**Status:** Accepted

## Decision

Use PostgreSQL as the only primary database technology for the initial platform. Enable Full-Text Search and pgvector in the same database.

Do not introduce a NoSQL/document database in the initial architecture.

## Rationale

The domain is strongly relational: claims, evidence, citations, topics, versions, languages, entities, and typed relationships benefit from transactional integrity, constraints, joins, and explicit schemas. PostgreSQL covers relational persistence, JSON metadata where unavoidable, semantic vectors, and keyword search without another operational datastore.

## Consequences

Positive: simpler operations, ACID publication workflows, consistent graph-like relations, easier migrations and analytics.

Trade-off: very high-scale search may eventually justify a separate search engine; that decision must be driven by measured requirements.
