import { describe, it, expect } from "vitest";
import {
  parseResearchList,
  parseResearchItem,
  InvalidResearchDataError,
  filterResearch,
  safeSourceUrl,
  uniqueSources,
} from "../lib/domain";
import { fixture } from "./fixture";
describe("publication contract", () => {
  it("parses a complete canonical record", () =>
    expect(parseResearchItem(fixture(), "en")).toEqual(fixture()));
  it("does not silently use another locale", () =>
    expect(() => parseResearchItem(fixture(), "pt-BR")).toThrow(
      InvalidResearchDataError,
    ));
  it("rejects missing synthetic flags and malformed dates", () => {
    expect(() =>
      parseResearchList(
        { items: [{ ...fixture(), isSynthetic: undefined }] },
        "en",
      ),
    ).toThrow();
    expect(() =>
      parseResearchList(
        { items: [{ ...fixture(), publishedAt: "tomorrow" }] },
        "en",
      ),
    ).toThrow();
  });
  it("rejects duplicate canonical records and route traversal slugs", () => {
    expect(() =>
      parseResearchList({ items: [fixture(), fixture()] }, "en"),
    ).toThrow();
    expect(() =>
      parseResearchItem({ ...fixture(), slug: "../secret" }, "en"),
    ).toThrow();
  });
  it("requires the current version to be represented in history", () =>
    expect(() =>
      parseResearchItem({ ...fixture(), versions: [] }, "en"),
    ).toThrow());
  it("rejects dangerous protocols and credential-bearing source links", () => {
    expect(safeSourceUrl("javascript:alert(1)")).toBeNull();
    expect(safeSourceUrl("data:text/html,hello")).toBeNull();
    expect(safeSourceUrl("https://user:password@example.org")).toBeNull();
    expect(safeSourceUrl("https://example.org/paper")).toBe(
      "https://example.org/paper",
    );
    const item = fixture();
    item.claims[0].evidence[0].source.url = "javascript:alert(1)";
    expect(() => parseResearchItem(item, "en")).toThrow();
  });
  it("deduplicates cited sources by canonical ID", () => {
    const item = fixture();
    item.claims.push({ ...item.claims[0], id: "claim-2" });
    expect(uniqueSources(item)).toHaveLength(1);
  });
  it("searches the API records with a combined topic and text filter", () => {
    expect(
      filterResearch([fixture()], "  EFFICIENT ", "Attention"),
    ).toHaveLength(1);
    expect(filterResearch([fixture()], "efficient", "Robotics")).toHaveLength(
      0,
    );
    expect(filterResearch([fixture()], "unknown", "")).toHaveLength(0);
  });
});
