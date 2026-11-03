import { getResearchItems } from "@/lib/api";
import { renderRss } from "@/lib/feed";
import { isLocale } from "@/lib/i18n";
import { siteOrigin } from "@/lib/urls";
export async function GET(
  _request: Request,
  { params }: { params: Promise<{ locale: string }> },
) {
  const { locale } = await params;
  if (!isLocale(locale)) return new Response("Not found", { status: 404 });
  try {
    const items = await getResearchItems(locale);
    return new Response(renderRss(items, locale, siteOrigin()), {
      headers: {
        "Content-Type": "application/rss+xml; charset=utf-8",
        "Cache-Control": "public, max-age=60, stale-while-revalidate=120",
      },
    });
  } catch {
    return new Response(
      "Research feed temporarily unavailable. Please retry.",
      {
        status: 503,
        headers: { "Retry-After": "60", "Cache-Control": "no-store" },
      },
    );
  }
}
