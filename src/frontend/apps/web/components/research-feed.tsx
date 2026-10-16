"use client";
import Link from "next/link";
import { useMemo, useState } from "react";
import { filterResearch, type ResearchItemSummary } from "@/lib/domain";
import { dictionary, localizedDate, term, type Locale } from "@/lib/i18n";
import { researchPath } from "@/lib/urls";
import { ArrowIcon, EvidenceMark, SearchIcon } from "./icons";

export function ResearchFeed({
  items,
  locale,
}: {
  items: ResearchItemSummary[];
  locale: Locale;
}) {
  const t = dictionary(locale);
  const [query, setQuery] = useState("");
  const [topic, setTopic] = useState("");
  const topics = useMemo(
    () => [...new Set(items.flatMap((item) => item.topics))].sort(),
    [items],
  );
  const results = useMemo(
    () => filterResearch(items, query, topic),
    [items, query, topic],
  );
  return (
    <>
      <div className="feed-tools">
        <div className="topic-filters" aria-label={t.topics}>
          <button
            className={topic === "" ? "chip selected" : "chip"}
            onClick={() => setTopic("")}
            aria-pressed={topic === ""}
          >
            {t.all}
          </button>
          {topics.map((value) => (
            <button
              key={value}
              className={topic === value ? "chip selected" : "chip"}
              aria-pressed={topic === value}
              onClick={() => setTopic(value)}
            >
              {value}
            </button>
          ))}
        </div>
        <label className="search-box">
          <SearchIcon />
          <span className="sr-only">{t.searchLabel}</span>
          <input
            type="search"
            placeholder={t.search}
            value={query}
            onChange={(event) => setQuery(event.target.value)}
          />
        </label>
      </div>
      <div className="feed-count" role="status" aria-live="polite">
        {results.length} {results.length === 1 ? t.publication : t.publications}
        {(query || topic) && (
          <button
            onClick={() => {
              setQuery("");
              setTopic("");
            }}
          >
            {t.reset} ×
          </button>
        )}
      </div>
      <div className="feed-grid">
        <div className="research-list">
          {results.length ? (
            results.map((item, index) => (
              <article className="research-card" key={item.id}>
                <div className="card-overline">
                  <span className="record-number">
                    {String(index + 1).padStart(2, "0")}
                  </span>
                  <span className="type-label">
                    {term(item.itemType, locale)}
                  </span>
                  <span className="card-date">
                    <time dateTime={item.publishedAt}>
                      {localizedDate(item.publishedAt, locale)}
                    </time>
                  </span>
                </div>
                <div className="card-body">
                  {item.isSynthetic && (
                    <span className="synthetic-badge">{t.synthetic}</span>
                  )}
                  <h3>
                    <Link href={researchPath(locale, item.slug)}>
                      {item.title}
                    </Link>
                  </h3>
                  <p>{item.summary}</p>
                  <div className="topic-tags">
                    {item.topics.map((value) => (
                      <span key={value}>{value}</span>
                    ))}
                  </div>
                </div>
                <div className="card-footer">
                  <div className="evidence-label">
                    <EvidenceMark />
                    <span>
                      {t.evidence}:{" "}
                      <strong>{term(item.evidenceStrength, locale)}</strong>
                    </span>
                  </div>
                  <Link
                    className="text-link"
                    href={researchPath(locale, item.slug)}
                  >
                    {t.read}
                    <ArrowIcon />
                  </Link>
                </div>
              </article>
            ))
          ) : (
            <div className="state-card">
              <h3>{items.length ? t.noResults : t.empty}</h3>
              <p>{items.length ? t.noResultsNote : t.emptyNote}</p>
              {items.length > 0 && (
                <button
                  className="button button-light"
                  onClick={() => {
                    setQuery("");
                    setTopic("");
                  }}
                >
                  {t.reset}
                </button>
              )}
            </div>
          )}
        </div>
        <aside className="feed-aside">
          <div className="reading-path">
            <span className="eyebrow">{t.readingChain}</span>
            <h3>{t.asideTitle}</h3>
            <p>{t.asideText}</p>
            <ol>
              {[
                [t.asideClaim, t.asideClaimText],
                [t.asideEvidence, t.asideEvidenceText],
                [t.asideSource, t.asideSourceText],
              ].map(([title, text], index) => (
                <li key={title}>
                  <span className="path-node">{index + 1}</span>
                  <div>
                    <strong>{title}</strong>
                    <span>{text}</span>
                  </div>
                </li>
              ))}
            </ol>
            <Link className="text-link" href={`/${locale}#approach`}>
              {t.methodology}
              <ArrowIcon />
            </Link>
          </div>
          <div className="bilingual-note">
            <span aria-hidden="true" className="language-monogram">
              A<span>á</span>
            </span>
            <h3>{t.bilingual}</h3>
            <p>{t.bilingualText}</p>
          </div>
        </aside>
      </div>
    </>
  );
}
