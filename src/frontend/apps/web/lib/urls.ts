import { locales, type Locale } from "./i18n";
export function siteOrigin(): string {
  const url = new URL(process.env.SITE_URL || "http://localhost:3000");
  if (
    !["https:", "http:"].includes(url.protocol) ||
    url.username ||
    url.password
  )
    throw new Error("SITE_URL must be an HTTP(S) origin.");
  return url.origin;
}
export function researchPath(locale: Locale, slug: string): string {
  return `/${locale}/research/${encodeURIComponent(slug)}`;
}
export function languageAlternates(
  slug?: string,
  availableLocales: readonly Locale[] = locales,
): Record<string, string> {
  const paths = Object.fromEntries(
    availableLocales.map((locale) => [
      locale,
      `${siteOrigin()}${slug ? researchPath(locale, slug) : `/${locale}`}`,
    ]),
  );
  return Object.keys(paths).length
    ? { ...paths, "x-default": paths["pt-BR"] || paths.en }
    : {};
}
