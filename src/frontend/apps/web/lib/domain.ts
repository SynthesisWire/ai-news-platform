import { isLocale, type Locale } from "./i18n";

export interface ResearchItemSummary {
  id: string;
  slug: string;
  locale: Locale;
  title: string;
  summary: string;
  itemType: string;
  evidenceStrength: string;
  publishedAt: string;
  isSynthetic: boolean;
  version: number;
  topics: string[];
}
export interface Source {
  id: string;
  title: string;
  url: string;
  publisher: string;
  publishedAt: string;
  isSynthetic: boolean;
}
export interface Evidence {
  id: string;
  kind: string;
  excerpt: string;
  locator: string;
  source: Source;
}
export interface Claim {
  id: string;
  statement: string;
  claimType: string;
  confidence: string;
  evidence: Evidence[];
}
export interface PublicationVersion {
  version: number;
  publishedAt: string;
  changeNote: string;
}
export interface ResearchItem extends ResearchItemSummary {
  whyItMatters: string;
  limitations: string[];
  claims: Claim[];
  versions: PublicationVersion[];
}
export class InvalidResearchDataError extends Error {
  constructor() {
    super("The research service returned an invalid publication.");
    this.name = "InvalidResearchDataError";
  }
}
function invalid(): never {
  throw new InvalidResearchDataError();
}
function object(value: unknown): Record<string, unknown> {
  if (!value || typeof value !== "object" || Array.isArray(value))
    return invalid();
  return value as Record<string, unknown>;
}
function string(value: unknown): string {
  return typeof value === "string" && value.trim() ? value : invalid();
}
function bool(value: unknown): boolean {
  return typeof value === "boolean" ? value : invalid();
}
function version(value: unknown): number {
  return typeof value === "number" && Number.isInteger(value) && value > 0
    ? value
    : invalid();
}
function array<T>(value: unknown, parse: (item: unknown) => T): T[] {
  return Array.isArray(value) ? value.map(parse) : invalid();
}
function date(value: unknown): string {
  const result = string(value);
  return /^\d{4}-\d{2}-\d{2}T/.test(result) &&
    Number.isFinite(Date.parse(result))
    ? result
    : invalid();
}
export function safeSourceUrl(value: string): string | null {
  try {
    const url = new URL(value);
    return ["https:", "http:"].includes(url.protocol) &&
      !url.username &&
      !url.password
      ? url.href
      : null;
  } catch {
    return null;
  }
}
export function parseSummary(
  value: unknown,
  expectedLocale: Locale,
): ResearchItemSummary {
  const item = object(value);
  const locale = string(item.locale);
  const slug = string(item.slug);
  if (
    !isLocale(locale) ||
    locale !== expectedLocale ||
    !/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(slug)
  )
    return invalid();
  return {
    id: string(item.id),
    slug,
    locale,
    title: string(item.title),
    summary: string(item.summary),
    itemType: string(item.itemType),
    evidenceStrength: string(item.evidenceStrength),
    publishedAt: date(item.publishedAt),
    isSynthetic: bool(item.isSynthetic),
    version: version(item.version),
    topics: array(item.topics, string),
  };
}
export function parseResearchList(
  value: unknown,
  locale: Locale,
): ResearchItemSummary[] {
  const result = array(object(value).items, (item) =>
    parseSummary(item, locale),
  );
  if (
    new Set(result.map((item) => item.id)).size !== result.length ||
    new Set(result.map((item) => item.slug)).size !== result.length
  )
    return invalid();
  return result;
}
function parseSource(value: unknown): Source {
  const source = object(value);
  const url = safeSourceUrl(string(source.url));
  if (!url) return invalid();
  return {
    id: string(source.id),
    title: string(source.title),
    url,
    publisher: string(source.publisher),
    publishedAt: date(source.publishedAt),
    isSynthetic: bool(source.isSynthetic),
  };
}
function parseEvidence(value: unknown): Evidence {
  const evidence = object(value);
  return {
    id: string(evidence.id),
    kind: string(evidence.kind),
    excerpt: string(evidence.excerpt),
    locator: string(evidence.locator),
    source: parseSource(evidence.source),
  };
}
function parseClaim(value: unknown): Claim {
  const claim = object(value);
  return {
    id: string(claim.id),
    statement: string(claim.statement),
    claimType: string(claim.claimType),
    confidence: string(claim.confidence),
    evidence: array(claim.evidence, parseEvidence),
  };
}
export function parseResearchItem(
  value: unknown,
  locale: Locale,
): ResearchItem {
  const item = object(value);
  const result = {
    ...parseSummary(item, locale),
    whyItMatters: string(item.whyItMatters),
    limitations: array(item.limitations, string),
    claims: array(item.claims, parseClaim),
    versions: array(item.versions, (value) => {
      const entry = object(value);
      return {
        version: version(entry.version),
        publishedAt: date(entry.publishedAt),
        changeNote: string(entry.changeNote),
      };
    }),
  };
  if (!result.versions.some((entry) => entry.version === result.version))
    return invalid();
  return result;
}
export function uniqueSources(item: ResearchItem): Source[] {
  return [
    ...new Map(
      item.claims.flatMap((claim) =>
        claim.evidence.map(({ source }) => [source.id, source] as const),
      ),
    ).values(),
  ];
}
export function filterResearch(
  items: ResearchItemSummary[],
  query: string,
  topic: string,
): ResearchItemSummary[] {
  const needle = query.trim().toLocaleLowerCase();
  return items.filter(
    (item) =>
      (!topic || item.topics.includes(topic)) &&
      `${item.title} ${item.summary} ${item.topics.join(" ")}`
        .toLocaleLowerCase()
        .includes(needle),
  );
}
