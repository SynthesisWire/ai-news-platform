# ADR-0003 — Event-Driven Workflows with Transactional Outbox

**Status:** Accepted

## Decision

Use Azure Service Bus for asynchronous integration and PostgreSQL Transactional Outbox for reliable event publication.

## Rationale

Research ingestion, model enrichment, translation, publication and channel distribution have different latency, failure, retry and scaling profiles. Async boundaries prevent external provider failures from blocking public/product workflows.

## Guarantees

- at-least-once delivery;
- idempotent consumers;
- event schema versioning;
- correlation/causation IDs;
- dead-letter handling;
- replay-safe publication.
