# 01 — System Context

```mermaid
flowchart LR
    Sources[External Sources\nPapers · Blogs · News · APIs · RSS · GitHub] --> Platform[AI Research Intelligence Platform]
    Notion[Existing Notion Research Hub] <--> Platform
    Editors[Editors / Researchers] --> Platform
    Platform --> Web[Public Website]
    Platform --> RSS[RSS / Atom]
    Platform --> X[X / Social]
    Platform --> Newsletter[Newsletter]
    Platform --> PublicAPI[Future Public API]
    Readers[Readers / Researchers] --> Web
    Readers --> RSS
    Readers --> PublicAPI
```

## Boundaries

The platform owns canonical content, claims, evidence, topics, relationships, translations, publication status, and version history.

Notion remains a supported editorial input surface during migration, but it is not queried by the public website at request time.
