import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getResearchItems } from "@/lib/api";
import { dictionary, isLocale } from "@/lib/i18n";
import { languageAlternates } from "@/lib/urls";
import { ResearchFeed } from "@/components/research-feed";
import { Unavailable } from "@/components/unavailable";
import { ArrowIcon } from "@/components/icons";

type Props = { params: Promise<{ locale: string }> };
export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { locale } = await params;
  if (!isLocale(locale)) return {};
  const t = dictionary(locale);
  return {
    title: t.brand,
    description: t.heroDescription,
    alternates: {
      canonical: `/${locale}`,
      languages: languageAlternates(),
      types: { "application/rss+xml": `/${locale}/rss.xml` },
    },
    openGraph: {
      title: t.brand,
      description: t.heroDescription,
      url: `/${locale}`,
      locale: locale === "en" ? "en_US" : "pt_BR",
      type: "website",
    },
  };
}
export default async function ResearchIndex({ params }: Props) {
  const { locale } = await params;
  if (!isLocale(locale)) notFound();
  const t = dictionary(locale);
  let items;
  try {
    items = await getResearchItems(locale);
  } catch {
    items = null;
  }
  return (
    <main id="main">
      <section className="hero shell">
        <div className="hero-copy">
          <div className="eyebrow">
            <span className="small-rule" />
            {t.eyebrow}
          </div>
          <h1>
            {t.heroTitle}
            <br />
            <em>{t.heroAccent}</em>
          </h1>
          <p>{t.heroDescription}</p>
          <a className="button button-dark" href="#research">
            {t.browse}
            <ArrowIcon />
          </a>
        </div>
        <div className="hero-visual" aria-hidden="true">
          <div className="orbit orbit-one" />
          <div className="orbit orbit-two" />
          <div className="visual-grid" />
          <div className="diagram-note note-one">
            <span className="note-index">01</span>
            <span>{t.asideClaim}</span>
            <span className="note-dot" />
          </div>
          <div className="diagram-note note-two">
            <span className="note-index">02</span>
            <span>{t.asideEvidence}</span>
            <span className="note-dot" />
          </div>
          <div className="diagram-note note-three">
            <span className="note-index">03</span>
            <span>{t.asideSource}</span>
            <span className="note-dot" />
          </div>
          <svg viewBox="0 0 420 300" className="diagram-lines">
            <path
              d="M125 80C280 80 120 150 255 150S130 235 280 235"
              fill="none"
              stroke="currentColor"
              strokeWidth="1.5"
              strokeDasharray="4 5"
            />
          </svg>
          <span className="diagram-caption">{t.connected}</span>
        </div>
      </section>
      <section className="research-section shell" id="research">
        <div className="section-heading">
          <div>
            <span className="eyebrow">{t.desk}</span>
            <h2>{t.latest}</h2>
            <p>{t.latestNote}</p>
          </div>
          <a className="rss-link" href={`/${locale}/rss.xml`}>
            <span aria-hidden="true">◔</span>
            {t.rss}
            <ArrowIcon />
          </a>
        </div>
        {items ? (
          <ResearchFeed items={items} locale={locale} />
        ) : (
          <Unavailable locale={locale} />
        )}
      </section>
      <section className="approach-section" id="approach">
        <div className="shell">
          <div className="approach-heading">
            <span className="eyebrow">{t.methodology}</span>
            <h2>{t.approachTitle}</h2>
            <p>{t.approachDescription}</p>
          </div>
          <div className="approach-grid">
            {[
              [t.approachOne, t.approachOneText],
              [t.approachTwo, t.approachTwoText],
              [t.approachThree, t.approachThreeText],
            ].map(([title, text], index) => (
              <div key={title}>
                <span className="approach-number">0{index + 1}</span>
                <h3>{title}</h3>
                <p>{text}</p>
              </div>
            ))}
          </div>
        </div>
      </section>
    </main>
  );
}
