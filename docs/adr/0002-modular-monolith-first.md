# ADR-0002 — Modular Monolith First

**Status:** Accepted

## Decision

Start backend domain/application logic as a modular monolith with strict bounded-context/module boundaries. Run ingestion/enrichment/publishing/distribution as independent workers where scaling characteristics differ.

## Rationale

Microservices would add distributed consistency, deployment and observability overhead before traffic/domain boundaries are validated. Explicit contracts and integration events retain a clean extraction path later.

## Extraction candidates

- ingestion at high source volume;
- AI enrichment when compute/provider workloads dominate;
- search when public query load requires isolation;
- distribution when many channels/integrations are added.
