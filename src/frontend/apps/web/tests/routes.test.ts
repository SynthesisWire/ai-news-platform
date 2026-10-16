import { afterEach, describe, it, expect, vi } from "vitest";
import { dictionary, isLocale, localizedDate, term } from "../lib/i18n";
import { languageAlternates, researchPath, siteOrigin } from "../lib/urls";
import { escapeXml, renderRss } from "../lib/feed";
import { fixture } from "./fixture";
afterEach(() => vi.unstubAllEnvs());
describe("localization and public routes", () => {
  it("only accepts explicit supported locales", () => {
    expect(isLocale("en")).toBe(true);
    expect(isLocale("pt-BR")).toBe(true);
    expect(isLocale("pt")).toBe(false);
  });
  it("has all translated strings and canonical vocabulary", () => {
    expect(Object.keys(dictionary("en"))).toEqual(
      Object.keys(dictionary("pt-BR")),
    );
    expect(term("synthetic-fixture", "pt-BR")).toBe("Dado sintético de teste");
  });
  it("uses UTC for publication dates", () =>
    expect(localizedDate("2026-10-15T23:59:00Z", "en")).toBe("Oct 15, 2026"));
  it("builds canonical paths and only verified locale alternates", () => {
    vi.stubEnv("SITE_URL", "https://research.example.org/");
    expect(researchPath("pt-BR", "attention")).toBe(
      "/pt-BR/research/attention",
    );
    expect(languageAlternates("attention", ["en"])).toEqual({
      en: "https://research.example.org/en/research/attention",
      "x-default": "https://research.example.org/en/research/attention",
    });
    expect(languageAlternates("attention", [])).toEqual({});
    expect(languageAlternates()["pt-BR"]).toBe(
      "https://research.example.org/pt-BR",
    );
  });
  it("rejects malformed public origins", () => {
    vi.stubEnv("SITE_URL", "javascript:alert(1)");
    expect(siteOrigin).toThrow();
  });
  it("escapes untrusted feed content and labels synthetic entries", () => {
    const item = {
      ...fixture(),
      title: '<script>&"',
      summary: "A & B",
      topics: ["<topic>"],
    };
    const xml = renderRss([item], "en", "https://example.org");
    expect(xml).toContain("[Synthetic example] &lt;script&gt;&amp;&quot;");
    expect(xml).toContain("fictional test data");
    expect(xml).toContain("&lt;topic&gt;");
    expect(xml).not.toContain("<script>");
    expect(escapeXml("'quoted' & value")).toBe(
      "&apos;quoted&apos; &amp; value",
    );
  });
});
