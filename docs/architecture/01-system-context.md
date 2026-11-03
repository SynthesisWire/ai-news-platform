# 01 — System Context

```mermaid
flowchart LR
    Sources[External Sources\nPapers · Blogs · News · APIs · RSS · GitHub] --> Platform[AI Research Intelligence Platform]
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


Editorial input is owned by the platform. The current local increment uses OS/database-authorized CLI commands; a hosted identity/authorization design is required before enabling editorial HTTP writes.
