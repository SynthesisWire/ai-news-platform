# 12 - First End-to-End Slice

Implemented scope: local administrative commands, not a hosted publishing operation.

```mermaid
flowchart LR
 URL[Allowed public HTTPS URL] --> Fetch[Bounded capture with pinned public IP]
 Fetch --> Snapshot[Immutable private snapshot and provenance]
 Snapshot --> Dedup[Exact dedup / material-match hint]
 Dedup --> Draft[Human-authored structured PT-BR and EN draft]
 Draft --> Review[Explicit human review of current revision]
 Review --> Canon[Canonical item and atomic outbox]
 Canon --> Web[Existing bilingual web and RSS]
```

Acceptance evidence and commands are in [local editorial operations](../local-editorial.md). Source bytes are never served by the public API. The operator supplies all claims, source metadata, localized text, limitations and reuse-rights confirmation. No AI provider or automatic translation is used. Exact URL/content retries are idempotent; whitespace-normalized cross-URL matches are review hints, not automatic semantic merges. Publication requires a separate explicit review transition and publishes both locales atomically. Published corrections continue to use immutable publication versions; post-publication editorial correction commands are outside this first local slice.
