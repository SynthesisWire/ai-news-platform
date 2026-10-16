import { afterEach, expect, it, vi } from "vitest";
import { fixture } from "./fixture";
vi.mock("../lib/api", () => ({ getResearchItems: vi.fn() }));
import { getResearchItems } from "../lib/api";
import { GET } from "../app/[locale]/rss.xml/route";
import sitemap from "../app/sitemap";
afterEach(() => vi.unstubAllEnvs());
it("returns a valid localized feed", async () => {
  vi.mocked(getResearchItems).mockResolvedValue([fixture()]);
  const response = await GET(new Request("http://localhost/en/rss.xml"), {
    params: Promise.resolve({ locale: "en" }),
  });
  expect(response.status).toBe(200);
  expect(response.headers.get("content-type")).toContain("application/rss+xml");
  expect(await response.text()).toContain("<language>en</language>");
});
it("rejects unsupported feed locales", async () =>
  expect(
    (
      await GET(new Request("http://localhost/fr/rss.xml"), {
        params: Promise.resolve({ locale: "fr" }),
      })
    ).status,
  ).toBe(404));
it("returns 503 rather than an empty successful feed on upstream failure", async () => {
  vi.mocked(getResearchItems).mockRejectedValue(new Error("secret"));
  const response = await GET(new Request("http://localhost/en/rss.xml"), {
    params: Promise.resolve({ locale: "en" }),
  });
  expect(response.status).toBe(503);
  expect(await response.text()).not.toContain("secret");
});
it("omits synthetic records and unpublished locale alternates from sitemap", async () => {
  vi.stubEnv("SITE_URL", "https://research.example.org");
  vi.mocked(getResearchItems).mockImplementation(async (locale) =>
    locale === "en"
      ? [{ ...fixture(), isSynthetic: false }]
      : [fixture("pt-BR")],
  );
  const entries = await sitemap();
  expect(entries).toHaveLength(3);
  const article = entries.find((entry) => entry.url.includes("/research/"));
  expect(article?.alternates?.languages).toEqual({
    en: "https://research.example.org/en/research/synthetic-efficient-attention",
    "x-default":
      "https://research.example.org/en/research/synthetic-efficient-attention",
  });
});
