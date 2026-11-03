import { afterEach, describe, expect, it, vi } from "vitest";
import {
  getResearchItem,
  getResearchItems,
  ResearchNotFoundError,
  ResearchServiceError,
} from "../lib/api";
import { fixture } from "./fixture";
afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});
describe("server API adapter", () => {
  it("uses the configured upstream and locale, without caching publications", async () => {
    vi.stubEnv("API_BASE_URL", "http://api:5080");
    const fetch = vi
      .fn()
      .mockResolvedValue(Response.json({ items: [fixture()] }));
    vi.stubGlobal("fetch", fetch);
    expect(await getResearchItems("en")).toHaveLength(1);
    expect(String(fetch.mock.calls[0][0])).toBe(
      "http://api:5080/api/research-items?locale=en",
    );
    expect(fetch.mock.calls[0][1].cache).toBe("no-store");
  });
  it("maps unpublished records to not-found", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(new Response(null, { status: 404 })),
    );
    await expect(getResearchItem("en", "missing")).rejects.toBeInstanceOf(
      ResearchNotFoundError,
    );
  });
  it("never substitutes fixtures when the API fails", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockRejectedValue(new Error("private upstream detail")),
    );
    await expect(getResearchItems("en")).rejects.toThrow(
      "The research service is unavailable.",
    );
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValue(
          new Response("private upstream detail", { status: 500 }),
        ),
    );
    await expect(getResearchItems("en")).rejects.toBeInstanceOf(
      ResearchServiceError,
    );
  });
  it("rejects mismatched publication slugs", async () => {
    vi.stubGlobal("fetch", vi.fn().mockResolvedValue(Response.json(fixture())));
    await expect(getResearchItem("en", "another-slug")).rejects.toBeInstanceOf(
      ResearchServiceError,
    );
  });
  it("does not request invalid slugs", async () => {
    const fetch = vi.fn();
    vi.stubGlobal("fetch", fetch);
    await expect(getResearchItem("en", "../private")).rejects.toBeInstanceOf(
      ResearchNotFoundError,
    );
    expect(fetch).not.toHaveBeenCalled();
  });
});
