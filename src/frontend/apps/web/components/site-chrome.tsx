import Link from "next/link";
import { dictionary, type Locale } from "@/lib/i18n";
import { BrandMark, ArrowIcon } from "./icons";
import { LanguageSwitcher } from "./language-switcher";

export function Header({ locale }: { locale: Locale }) {
  const t = dictionary(locale);
  return (
    <>
      <div className="preview-bar">
        <div className="shell preview-inner">
          <span className="preview-label">
            <span className="status-dot" />
            {t.preview}
          </span>
          <span>{t.previewNote}</span>
        </div>
      </div>
      <header className="site-header shell">
        <Link
          className="brand"
          href={`/${locale}`}
          aria-label={`${t.brand} / ${t.brandSub}`}
        >
          <BrandMark />
          <span>
            <strong>{t.brand}</strong>
            <small>{t.brandSub}</small>
          </span>
        </Link>
        <nav
          aria-label={
            locale === "en" ? "Main navigation" : "Navegação principal"
          }
          className="header-nav"
        >
          <Link className="nav-link active" href={`/${locale}`}>
            {t.research}
          </Link>
          <Link className="nav-link approach-link" href={`/${locale}#approach`}>
            {t.methodology}
          </Link>
          <LanguageSwitcher locale={locale} />
        </nav>
      </header>
    </>
  );
}
export function Footer({ locale }: { locale: Locale }) {
  const t = dictionary(locale);
  return (
    <footer className="site-footer">
      <div className="shell footer-top">
        <div>
          <Link className="footer-brand" href={`/${locale}`}>
            <BrandMark />
            <span>
              {t.brand}
              <small>{t.brandSub}</small>
            </span>
          </Link>
          <p>{t.footerNote}</p>
        </div>
        <a className="text-link" href={`/${locale}/rss.xml`}>
          {t.rss}
          <ArrowIcon />
        </a>
      </div>
      <div className="shell footer-bottom">
        <span>{t.status}</span>
        <span>EN · PT-BR</span>
      </div>
    </footer>
  );
}
