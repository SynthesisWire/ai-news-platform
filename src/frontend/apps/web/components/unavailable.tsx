import Link from "next/link";
import { dictionary, type Locale } from "@/lib/i18n";
import { RetryButton } from "./retry-button";
export function Unavailable({ locale }: { locale: Locale }) {
  const t = dictionary(locale);
  return (
    <section className="state-card unavailable" role="alert">
      <div className="state-symbol" aria-hidden="true">
        ↻
      </div>
      <h2>{t.unavailable}</h2>
      <p>{t.unavailableNote}</p>
      <div className="state-actions">
        <RetryButton label={t.retry} />
        <Link className="text-link" href={`/${locale}`}>
          {t.back}
        </Link>
      </div>
    </section>
  );
}
