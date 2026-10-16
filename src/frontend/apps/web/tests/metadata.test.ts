import { afterEach, expect, it, vi } from "vitest";
import { fixture } from "./fixture";
vi.mock("../lib/api", () => ({
  getResearchItem: vi.fn(),
  ResearchNotFoundError: class extends Error {},
}));
import { getResearchItem } from "../lib/api";
import { generateMetadata } from "../app/[locale]/research/[slug]/page";
import robots from "../app/robots";
afterEach(() => vi.unstubAllEnvs());
it("does not advertise a missing translation in article hreflang", async () => {
  vi.mocked(getResearchItem).mockImplementation(async (locale) => {
    if (locale === "pt-BR") throw new Error("Unpublished");
    return fixture();
  });
  vi.stubEnv("SITE_URL", "https://research.example.org");
  const metadata = await generateMetadata({
    params: Promise.resolve({ locale: "en", slug: fixture().slug }),
  });
  expect(metadata.alternates?.languages).toEqual({
    en: "https://research.example.org/en/research/synthetic-efficient-attention",
    "x-default":
      "https://research.example.org/en/research/synthetic-efficient-attention",
  });
  expect(metadata.robots).toEqual({ index: false, follow: true });
});
it("robots uses the runtime public origin", () => {
  vi.stubEnv("SITE_URL", "https://custom.example.org:8443");
  expect(robots().sitemap).toBe("https://custom.example.org:8443/sitemap.xml");
});
