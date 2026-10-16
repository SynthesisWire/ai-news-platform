import type { ResearchItem } from "../lib/domain";
// Synthetic contract fixture. This module is never imported by application code.
export function fixture(locale: "en" | "pt-BR" = "en"): ResearchItem {
  return {
    id: "synthetic-research-1",
    slug: "synthetic-efficient-attention",
    locale,
    title:
      locale === "en"
        ? "Synthetic efficient attention"
        : "Atenção eficiente sintética",
    summary: "Synthetic demonstration of a canonical evidence record.",
    itemType: "research-paper",
    evidenceStrength: "synthetic",
    publishedAt: "2026-10-15T10:00:00Z",
    isSynthetic: true,
    version: 1,
    topics: ["Efficiency", "Attention"],
    whyItMatters: "This fictional item tests the publication flow.",
    limitations: ["All results are fictional."],
    claims: [
      {
        id: "claim-1",
        statement: "The synthetic method reports a fictional result.",
        claimType: "synthetic-illustration",
        confidence: "low",
        evidence: [
          {
            id: "evidence-1",
            kind: "synthetic-fixture",
            excerpt: "This is not a real experiment.",
            locator: "Section 1, synthetic fixture",
            source: {
              id: "source-1",
              title: "Synthetic source",
              url: "https://example.org/synthetic",
              publisher: "Example Publisher",
              publishedAt: "2026-10-14T10:00:00Z",
              isSynthetic: true,
            },
          },
        ],
      },
    ],
    versions: [
      {
        version: 1,
        publishedAt: "2026-10-15T10:00:00Z",
        changeNote: "Initial synthetic publication.",
      },
    ],
  };
}
