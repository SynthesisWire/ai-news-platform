// Against the explicitly seeded local stack. No credentials or third-party requests.
import assert from "node:assert/strict";

const api = process.env.API_BASE_URL ?? "http://127.0.0.1:5080";
const web = process.env.WEB_BASE_URL ?? "http://127.0.0.1:3000";
const slug = "synthetic-efficient-attention";
const get = (base, path) => fetch(new URL(path, base), { signal: AbortSignal.timeout(15_000) });

for (const path of ["/health/live", "/health/ready"]) {
  assert.equal((await get(api, path)).status, 200, path);
}
let canonicalId;
for (const locale of ["en", "pt-BR"]) {
  const listingResponse = await get(api, `/api/research-items?locale=${locale}`);
  assert.equal(listingResponse.status, 200);
  const listing = await listingResponse.json();
  const item = listing.items.find((entry) => entry.slug === slug);
  assert.ok(item, `Synthetic fixture exists in ${locale}`);
  assert.equal(item.isSynthetic, true);
  assert.equal(item.locale, locale);
  canonicalId ??= item.id;
  assert.equal(item.id, canonicalId, "Both locales project one canonical record");

  const response = await get(api, `/api/research-items/${slug}?locale=${locale}`);
  assert.equal(response.status, 200);
  const detail = await response.json();
  assert.ok(detail.claims.length > 0);
  assert.ok(detail.limitations.length > 0);
  assert.ok(detail.versions.length > 0);
  for (const claim of detail.claims) {
    assert.ok(claim.evidence.length > 0, "Every claim has evidence");
    for (const evidence of claim.evidence) {
      assert.equal(evidence.source.isSynthetic, true);
      assert.equal(new URL(evidence.source.url).hostname, "example.org");
    }
  }

  const feed = await get(web, `/${locale}/rss.xml`);
  assert.equal(feed.status, 200);
  const feedXml = await feed.text();
  assert.ok(feedXml.includes("<rss"));
  assert.ok(feedXml.includes(`/${locale}/research/${slug}`));

  for (const path of [`/${locale}`, `/${locale}/research/${slug}`]) {
    const page = await get(web, path);
    assert.equal(page.status, 200, path);
    assert.ok((await page.text()).includes(item.title), `${path} renders API title`);
  }
}
const sitemap = await get(web, "/sitemap.xml");
assert.equal(sitemap.status, 200);
const sitemapXml = await sitemap.text();
assert.ok(sitemapXml.includes("/en</loc>"));
assert.ok(sitemapXml.includes("/pt-BR</loc>"));
assert.ok(!sitemapXml.includes(`/research/${slug}`), "Synthetic articles stay out of the sitemap");
assert.equal((await get(api, "/api/research-items?locale=fr")).status, 400);
assert.equal((await get(api, "/api/research-items/does-not-exist?locale=en")).status, 404);
assert.equal((await get(web, "/en/research/does-not-exist")).status, 404);
console.log("Live stack smoke passed: health, bilingual canonical content, evidence provenance, web, validation, and 404s.");
