# 06 — Multilingual Content Flow

```mermaid
flowchart TB
    Canon[Canonical Semantic Record\nclaims · evidence · entities · citations · relationships]
    Canon --> PT[PT-BR Editorial Rendering]
    Canon --> EN[English Editorial Rendering]
    PT --> PTReview[Locale Review]
    EN --> ENReview[Locale Review]
    PTReview --> Publish[Publication Version]
    ENReview --> Publish
```

## Immutable across languages

- source citations;
- numerical evidence;
- entity identifiers;
- benchmark/model/paper names;
- evidence strength;
- typed relationships.

## Localized

- headline;
- summary;
- explanations;
- examples;
- SEO description;
- social copy.
