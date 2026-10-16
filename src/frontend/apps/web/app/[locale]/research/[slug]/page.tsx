import type { Metadata } from "next";
import { brandName } from "@/lib/brand";
import Link from "next/link";
import { notFound } from "next/navigation";
import { getResearchItem, ResearchNotFoundError } from "@/lib/api";
import { uniqueSources } from "@/lib/domain";
import { dictionary, isLocale, localizedDate, term, locales } from "@/lib/i18n";
import { languageAlternates, researchPath } from "@/lib/urls";
import { ArrowIcon, EvidenceMark, LinkIcon } from "@/components/icons";
import { Unavailable } from "@/components/unavailable";

type Props = { params: Promise<{ locale: string; slug: string }> };
export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale, slug } = await params;
  if (!isLocale(locale)) notFound();
  try {
    const item = await getResearchItem(locale, slug);
    const available = (
      await Promise.all(
        locales.map(async (candidate) => {
          if (candidate === locale) return candidate;
          try {
            const alternate = await getResearchItem(candidate, slug);
            return alternate.id === item.id ? candidate : null;
          } catch {
            return null;
          }
        }),
      )
    ).filter((candidate) => candidate !== null);
    return {
      title: item.title,
      description: item.summary,
      alternates: {
        canonical: researchPath(locale, slug),
        languages: languageAlternates(slug, available),
      },
      robots: item.isSynthetic
        ? { index: false, follow: true }
        : { index: true, follow: true },
      openGraph: {
        title: item.title,
        siteName: brandName,
        description: item.summary,
        type: "article",
        publishedTime: item.publishedAt,
        url: researchPath(locale, slug),
        locale: locale === "en" ? "en_US" : "pt_BR",
      },
    };
  } catch (error) {
    if (error instanceof ResearchNotFoundError) notFound();
    return { title: dictionary(locale).unavailable, robots: { index: false } };
  }
}
export default async function ResearchDetail({ params }: Props) {
  const { locale, slug } = await params;
  if (!isLocale(locale)) notFound();
  const t = dictionary(locale);
  let item;
  try {
    item = await getResearchItem(locale, slug);
  } catch (error) {
    if (error instanceof ResearchNotFoundError) notFound();
    return (
      <main id="main" className="shell error-main">
        <Unavailable locale={locale} />
      </main>
    );
  }
  const sources = uniqueSources(item);
  const sourceNumber = (id: string) =>
    sources.findIndex((source) => source.id === id) + 1;
  return (
    <main id="main" className="shell detail-main">
      <Link className="back-link" href={`/${locale}`}>
        <ArrowIcon />
        {t.back}
      </Link>
      <article>
        <header className="article-header">
          <div className="article-overline">
            <span className="type-label">{term(item.itemType, locale)}</span>
            <span className="overline-divider" />
            <time dateTime={item.publishedAt}>
              {localizedDate(item.publishedAt, locale)}
            </time>
            <span className="version-tag">v{item.version}</span>
          </div>
          <h1>{item.title}</h1>
          <p className="article-summary">{item.summary}</p>
          <div className="topic-tags">
            {item.topics.map((topic) => (
              <span key={topic}>{topic}</span>
            ))}
          </div>
        </header>
        {item.isSynthetic && (
          <div className="synthetic-notice" role="note">
            <span className="synthetic-badge">{t.synthetic}</span>
            <p>{t.syntheticDetail}</p>
          </div>
        )}
        <div className="detail-grid">
          <div className="article-content">
            <section className="why-section" id="why">
              <span className="section-index">01 / {t.contextIndex}</span>
              <h2>{t.why}</h2>
              <p>{item.whyItMatters}</p>
            </section>
            <section className="article-section" id="claims">
              <div className="article-section-heading">
                <span className="section-index">02 / {t.evidenceIndex}</span>
                <h2>{t.claims}</h2>
                <p>{t.claimsNote}</p>
                <p className="canonical-note">{t.canonicalNote}</p>
              </div>
              <div className="claim-list">
                {item.claims.map((claim, index) => (
                  <article
                    className="claim-card"
                    key={claim.id}
                    id={`claim-${claim.id}`}
                  >
                    <div className="claim-top">
                      <span className="claim-number">
                        {t.claim} {String(index + 1).padStart(2, "0")}
                      </span>
                      <span className="claim-type">
                        {term(claim.claimType, locale)}
                      </span>
                    </div>
                    <h3>{claim.statement}</h3>
                    <div className="confidence-label">
                      <EvidenceMark />
                      {t.confidence}:{" "}
                      <strong>{term(claim.confidence, locale)}</strong>
                    </div>
                    <div className="evidence-list">
                      <h4>{t.supportingEvidence}</h4>
                      {claim.evidence.length ? (
                        claim.evidence.map((evidence) => (
                          <div className="evidence-entry" key={evidence.id}>
                            <div className="evidence-kind">
                              <span className="small-square" />
                              {term(evidence.kind, locale)}
                            </div>
                            <blockquote>{evidence.excerpt}</blockquote>
                            <div className="citation">
                              <a
                                href={`#source-${sourceNumber(evidence.source.id)}`}
                              >
                                [{sourceNumber(evidence.source.id)}]{" "}
                                {evidence.source.publisher}
                              </a>
                              <span>{evidence.locator}</span>
                            </div>
                          </div>
                        ))
                      ) : (
                        <p>{t.noEvidence}</p>
                      )}
                    </div>
                  </article>
                ))}
              </div>
            </section>
            <section className="limitations-section" id="limitations">
              <span className="section-index">03 / {t.limitationsIndex}</span>
              <h2>{t.limitations}</h2>
              {item.limitations.length ? (
                <ul>
                  {item.limitations.map((limitation, index) => (
                    <li key={index}>{limitation}</li>
                  ))}
                </ul>
              ) : (
                <p>{t.noLimitations}</p>
              )}
            </section>
            <section className="article-section" id="sources">
              <span className="section-index">04 / {t.sourcesIndex}</span>
              <h2>
                {t.sources}
                <span className="heading-count">{sources.length}</span>
              </h2>
              <ol className="source-list">
                {sources.map((source, index) => (
                  <li key={source.id} id={`source-${index + 1}`}>
                    <span className="source-number">
                      {String(index + 1).padStart(2, "0")}
                    </span>
                    <div className="source-content">
                      <span className="source-publisher">
                        {source.publisher}
                      </span>
                      <h3>
                        <a
                          href={source.url}
                          target="_blank"
                          rel="noopener noreferrer"
                        >
                          {source.title}
                          <LinkIcon />
                        </a>
                      </h3>
                      <p>
                        {t.sourceDate}{" "}
                        <time dateTime={source.publishedAt}>
                          {localizedDate(source.publishedAt, locale)}
                        </time>
                      </p>
                      {source.isSynthetic && (
                        <span className="synthetic-badge">{t.synthetic}</span>
                      )}
                    </div>
                  </li>
                ))}
              </ol>
            </section>
            <section className="article-section history-section" id="history">
              <span className="section-index">05 / {t.recordIndex}</span>
              <h2>{t.history}</h2>
              <p>{t.historyNote}</p>
              <ol className="version-list">
                {[...item.versions]
                  .sort((a, b) => b.version - a.version)
                  .map((entry) => (
                    <li key={entry.version}>
                      <span className="version-node" />
                      <div>
                        <div className="version-meta">
                          <strong>
                            {t.version} {entry.version}
                          </strong>
                          <time dateTime={entry.publishedAt}>
                            {localizedDate(entry.publishedAt, locale)}
                          </time>
                        </div>
                        <p>{entry.changeNote}</p>
                      </div>
                    </li>
                  ))}
              </ol>
            </section>
          </div>
          <aside className="article-sidebar">
            <div className="record-panel">
              <span className="eyebrow">{t.record}</span>
              <div className="record-strength">
                <EvidenceMark />
                <span>{t.evidence}</span>
                <strong>{term(item.evidenceStrength, locale)}</strong>
              </div>
              <dl>
                <div>
                  <dt>{t.published}</dt>
                  <dd>{localizedDate(item.publishedAt, locale)}</dd>
                </div>
                <div>
                  <dt>{t.version}</dt>
                  <dd>{item.version}</dd>
                </div>
                <div>
                  <dt>{t.claims}</dt>
                  <dd>{item.claims.length}</dd>
                </div>
                <div>
                  <dt>{t.sources}</dt>
                  <dd>{sources.length}</dd>
                </div>
              </dl>
              <nav aria-label={t.onThisPage}>
                <h3>{t.onThisPage}</h3>
                {[
                  ["why", t.why],
                  ["claims", t.claims],
                  ["limitations", t.limitations],
                  ["sources", t.sources],
                  ["history", t.history],
                ].map(([id, label]) => (
                  <a href={`#${id}`} key={id}>
                    {label}
                    <span aria-hidden="true">↗</span>
                  </a>
                ))}
              </nav>
            </div>
          </aside>
        </div>
      </article>
    </main>
  );
}
