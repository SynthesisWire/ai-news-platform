# 14 — Initial Non-Functional Requirements

## Availability

- Public read path target: 99.9% after MVP stabilization.
- Publishing/editorial can tolerate lower availability than public read.

## Performance

- Public cached page TTFB target < 300 ms at edge where cached.
- API p95 simple reads < 250 ms in-region.
- Search p95 initial target < 500 ms.
- Ingestion/enrichment are asynchronous and not user-request critical paths.

## Scalability

- Stateless Web/API replicas.
- Queue-depth-based independent worker scaling.
- PostgreSQL read replica option for public workloads.
- Expensive AI tasks isolated from publication path.

## Reliability

- Idempotent event consumers.
- Transactional Outbox.
- Dead-letter queues.
- Replayable source snapshots.
- Immutable publication versions.

## Security

- Source content is untrusted input.
- Never execute source-provided code/instructions.
- Separate fetch, enrichment, editorial and publication authorities.
- Managed identities and Key Vault for secrets.
- WAF/rate limits on public endpoints.

## Editorial integrity

- Every public claim can be traced to one or more evidence/source versions.
- Vendor claims and independent evidence are distinguishable.
- Corrections never rewrite history silently.
