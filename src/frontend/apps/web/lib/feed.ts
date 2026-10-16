import { brandTitle } from "./brand";
import type { ResearchItemSummary } from "./domain";
import { dictionary, type Locale } from "./i18n";
import { researchPath } from "./urls";
export function escapeXml(value: string): string {
  return value.replace(
    /[<>&'\"]/g,
    (character) =>
      ({
        "<": "&lt;",
        ">": "&gt;",
        "&": "&amp;",
        "'": "&apos;",
        '"': "&quot;",
      })[character]!,
  );
}
export function renderRss(
  items: ResearchItemSummary[],
  locale: Locale,
  origin: string,
): string {
  const t = dictionary(locale);
  const channelUrl = `${origin}/${locale}`;
  return `<?xml version="1.0" encoding="UTF-8"?>\n<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom"><channel><title>${escapeXml(brandTitle)}</title><link>${escapeXml(channelUrl)}</link><description>${escapeXml(t.heroDescription)}</description><language>${locale}</language><atom:link href="${escapeXml(`${channelUrl}/rss.xml`)}" rel="self" type="application/rss+xml"/>${items
    .map((item) => {
      const url = `${origin}${researchPath(locale, item.slug)}`;
      const title = item.isSynthetic
        ? `[${t.synthetic}] ${item.title}`
        : item.title;
      return `<item><title>${escapeXml(title)}</title><link>${escapeXml(url)}</link><guid isPermaLink="true">${escapeXml(url)}</guid><description>${escapeXml(item.isSynthetic ? `${t.syntheticDetail} ${item.summary}` : item.summary)}</description><pubDate>${new Date(item.publishedAt).toUTCString()}</pubDate>${item.topics.map((topic) => `<category>${escapeXml(topic)}</category>`).join("")}</item>`;
    })
    .join("")}</channel></rss>`;
}
