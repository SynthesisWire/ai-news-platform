"use client";
import { useParams } from "next/navigation";
import { dictionary, isLocale } from "@/lib/i18n";
export default function ErrorPage({
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  const params = useParams();
  const value = String(params.locale);
  const t = dictionary(isLocale(value) ? value : "pt-BR");
  return (
    <main id="main" className="shell error-main">
      <section className="state-card" role="alert">
        <h1>{t.unavailable}</h1>
        <p>{t.unavailableNote}</p>
        <button className="button button-dark" onClick={reset}>
          {t.retry}
        </button>
      </section>
    </main>
  );
}
