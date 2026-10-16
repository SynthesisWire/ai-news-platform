import { test, expect } from "@playwright/test";

// HTTP-level rendering checks also run when a CI/container has no browser runtime.
test("server-rendered locale pages expose published API records and metadata", async ({
  request,
}) => {
  for (const locale of ["en", "pt-BR"]) {
    const index = await request.get(`/${locale}`);
    expect(index.status()).toBe(200);
    const indexHtml = await index.text();
    expect(indexHtml).toContain(`lang="${locale}"`);
    expect(indexHtml).toContain("synthetic-efficient-attention");
    expect(indexHtml).toContain('type="search"');
    const detail = await request.get(
      `/${locale}/research/synthetic-efficient-attention`,
    );
    expect(detail.status()).toBe(200);
    const detailHtml = await detail.text();
    for (const id of ["claims", "limitations", "sources", "history"]) {
      expect(detailHtml).toContain(`id="${id}"`);
    }
    expect(detailHtml).toContain('name="robots" content="noindex, follow"');
    expect(detailHtml).toContain('rel="canonical"');
    expect(detailHtml).toContain('hrefLang="en"');
    expect(detailHtml).toContain('hrefLang="pt-BR"');
    expect(detailHtml).not.toContain("Research is temporarily unavailable");
  }
});

test("server-rendered unpublished records and unsupported locale return HTTP 404", async ({
  request,
}) => {
  const missing = await request.get("/en/research/no-such-publication");
  expect(missing.status()).toBe(404);
  expect(await missing.text()).toContain("This research page isn’t available");
  expect((await request.get("/fr")).status()).toBe(404);
});

test("server-rendered outage never substitutes synthetic success", async ({
  request,
}) => {
  test.skip(
    Boolean(process.env.E2E_BASE_URL),
    "Fault injection only targets the isolated fixture API.",
  );
  await request.post("http://127.0.0.1:5109/__test/control?unavailable=true");
  try {
    const response = await request.get("/en");
    const html = await response.text();
    expect(html).toContain("Research is temporarily unavailable");
    expect(html).not.toContain('class="research-card"');
    expect((await request.get("/en/rss.xml")).status()).toBe(503);
  } finally {
    await request.post(
      "http://127.0.0.1:5109/__test/control?unavailable=false",
    );
  }
  expect(await (await request.get("/en")).text()).toContain(
    'class="research-card"',
  );
});
