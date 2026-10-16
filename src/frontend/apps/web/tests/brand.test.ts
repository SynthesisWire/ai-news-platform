import { describe, expect, it } from "vitest";
import { dictionary, locales } from "../lib/i18n";
import { renderRss } from "../lib/feed";
import { brandName, brandTagline, brandTitle } from "../lib/brand";
import { generateMetadata } from "../app/[locale]/page";
import { metadata } from "../app/layout";

describe("SynthesisWire identity", () => {
  it("retains the exact chosen brand and tagline in both locales", () => {
    expect(brandName).toBe("SynthesisWire");
    expect(brandTagline).toBe("Editorial Intelligence");
    for (const locale of locales) {
      expect(dictionary(locale).brand).toBe("SynthesisWire");
      expect(dictionary(locale).brandSub).toBe("Editorial Intelligence");
      expect(renderRss([], locale, "https://example.org")).toContain(
        "<title>SynthesisWire / Editorial Intelligence</title>",
      );
    }
  });
  it("uses the brand in root metadata and localized page metadata", async () => {
    expect(metadata.title).toEqual({
      default: "SynthesisWire / Editorial Intelligence",
      template: "%s · SynthesisWire",
    });
    for (const locale of locales) {
      const pageMetadata = await generateMetadata({
        params: Promise.resolve({ locale }),
      });
      expect(pageMetadata.title).toEqual({ absolute: brandTitle });
      expect(pageMetadata.openGraph).toMatchObject({
        title: "SynthesisWire / Editorial Intelligence",
        siteName: "SynthesisWire",
      });
    }
  });
});
