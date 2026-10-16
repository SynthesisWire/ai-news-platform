import "server-only";
import { cache } from "react";
import {
  parseResearchItem,
  parseResearchList,
  type ResearchItem,
  type ResearchItemSummary,
} from "./domain";
import type { Locale } from "./i18n";

export class ResearchServiceError extends Error {
  constructor(public readonly status?: number) {
    super("The research service is unavailable.");
    this.name = "ResearchServiceError";
  }
}
export class ResearchNotFoundError extends Error {
  constructor() {
    super("Publication not found.");
    this.name = "ResearchNotFoundError";
  }
}
function apiUrl(path: string, locale: Locale): URL {
  const base = new URL(process.env.API_BASE_URL || "http://localhost:5080");
  if (
    !["http:", "https:"].includes(base.protocol) ||
    base.username ||
    base.password
  )
    throw new ResearchServiceError();
  const url = new URL(path, base);
  url.searchParams.set("locale", locale);
  return url;
}
async function request(path: string, locale: Locale): Promise<unknown> {
  try {
    const response = await fetch(apiUrl(path, locale), {
      headers: { Accept: "application/json" },
      cache: "no-store",
      signal: AbortSignal.timeout(8_000),
    });
    if (response.status === 404) throw new ResearchNotFoundError();
    if (!response.ok) throw new ResearchServiceError(response.status);
    return await response.json();
  } catch (error) {
    if (
      error instanceof ResearchNotFoundError ||
      error instanceof ResearchServiceError
    )
      throw error;
    throw new ResearchServiceError();
  }
}
export const getResearchItems = cache(
  async (locale: Locale): Promise<ResearchItemSummary[]> => {
    return parseResearchList(
      await request("/api/research-items", locale),
      locale,
    );
  },
);
export const getResearchItem = cache(
  async (locale: Locale, slug: string): Promise<ResearchItem> => {
    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug))
      throw new ResearchNotFoundError();
    const item = parseResearchItem(
      await request(`/api/research-items/${encodeURIComponent(slug)}`, locale),
      locale,
    );
    if (item.slug !== slug) throw new ResearchServiceError();
    return item;
  },
);
