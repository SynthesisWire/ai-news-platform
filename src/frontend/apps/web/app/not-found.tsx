import Link from "next/link";
import { headers } from "next/headers";
import { dictionary, isLocale } from "@/lib/i18n";
export default async function NotFound() {
  const value = (await headers()).get("x-site-locale") || "pt-BR";
  const locale = isLocale(value) ? value : "pt-BR";
  const t = dictionary(locale);
  return (
    <main id="main" className="shell error-main">
      <section className="state-card">
        <span className="eyebrow">404</span>
        <h1>{t.notFound}</h1>
        <p>{t.notFoundNote}</p>
        <Link className="button button-dark" href={`/${locale}`}>
          {t.back}
        </Link>
      </section>
    </main>
  );
}
