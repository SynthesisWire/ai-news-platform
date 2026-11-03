"use client";
import { usePathname } from "next/navigation";
import { type Locale } from "@/lib/i18n";
export function LanguageSwitcher({ locale }: { locale: Locale }) {
  const pathname = usePathname();
  const suffix = pathname.replace(/^\/(en|pt-BR)(?=\/|$)/, "");
  // Full navigation keeps the root document's SSR language and metadata in sync.
  return (
    <div
      className="language-switch"
      aria-label={locale === "en" ? "Language" : "Idioma"}
    >
      <a
        href={`/en${suffix}`}
        lang="en"
        hrefLang="en"
        aria-current={locale === "en" ? "page" : undefined}
      >
        EN
      </a>
      <a
        href={`/pt-BR${suffix}`}
        lang="pt-BR"
        hrefLang="pt-BR"
        aria-current={locale === "pt-BR" ? "page" : undefined}
      >
        PT-BR
      </a>
    </div>
  );
}
