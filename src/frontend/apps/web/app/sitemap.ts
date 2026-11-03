import type { MetadataRoute } from "next";
import { getResearchItems } from "@/lib/api";
import { locales } from "@/lib/i18n";
import { languageAlternates, researchPath, siteOrigin } from "@/lib/urls";
export const dynamic = "force-dynamic";
export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const lists = await Promise.all(
    locales.map(async (locale) => ({
      locale,
      items: await getResearchItems(locale),
    })),
  );
  return lists.flatMap(({ locale, items }) => [
    {
      url: `${siteOrigin()}/${locale}`,
      alternates: { languages: languageAlternates() },
    },
    // Synthetic detail records are noindex and must not be promoted into a sitemap.
    ...items
      .filter((item) => !item.isSynthetic)
      .map((item) => ({
        url: `${siteOrigin()}${researchPath(locale, item.slug)}`,
        lastModified: item.publishedAt,
        alternates: {
          languages: languageAlternates(
            item.slug,
            lists
              .filter((list) =>
                list.items.some(
                  (candidate) =>
                    candidate.id === item.id &&
                    candidate.slug === item.slug &&
                    !candidate.isSynthetic,
                ),
              )
              .map((list) => list.locale),
          ),
        },
      })),
  ]);
}
