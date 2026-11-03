import { test, expect } from "@playwright/test";

const isolated = !process.env.E2E_BASE_URL;
test.beforeEach(async ({ request }) => {
  if (isolated)
    await request.post(
      "http://127.0.0.1:5109/__test/control?unavailable=false",
    );
});

test("research can be filtered and read with explicit synthetic labels", async ({
  page,
}, testInfo) => {
  await page.goto("/en");
  await expect(page.locator("html")).toHaveAttribute("lang", "en");
  await expect(page.getByRole("heading", { level: 1 })).toContainText(
    "Beyond the headline",
  );
  await expect(page.locator(".research-card").first()).toBeVisible();
  await expect(
    page.locator(".research-card").first().getByText("Synthetic example"),
  ).toBeVisible();
  await page.getByRole("searchbox").fill("no-match-zzzz");
  await expect(
    page.getByRole("heading", { name: "No matching research" }),
  ).toBeVisible();
  await page.getByRole("searchbox").fill("");
  await page.screenshot({
    path: testInfo.outputPath("research-list.png"),
    fullPage: true,
  });
  await page.locator(".research-card h3 a").first().click();
  await expect(page).toHaveURL(
    /\/en\/research\/synthetic-efficient-attention$/,
  );
  await expect(
    page.getByRole("heading", { name: "Claims & evidence", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Limitations & open questions" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: /Primary sources/ }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Publication history" }),
  ).toBeVisible();
  await expect(page.locator(".synthetic-notice")).toContainText(
    "fictional test data",
  );
  await expect(page.locator('meta[name="robots"]')).toHaveAttribute(
    "content",
    /noindex/,
  );
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBe(true);
  await page.screenshot({
    path: testInfo.outputPath("research-detail.png"),
    fullPage: true,
  });
});

test("language switching preserves the research record and document language", async ({
  page,
}) => {
  await page.goto("/en/research/synthetic-efficient-attention");
  await page.getByRole("link", { name: "PT-BR", exact: true }).click();
  await expect(page).toHaveURL(
    /\/pt-BR\/research\/synthetic-efficient-attention$/,
  );
  await expect(page.locator("html")).toHaveAttribute("lang", "pt-BR");
  await expect(
    page.getByRole("heading", { name: "Alegações e evidências", exact: true }),
  ).toBeVisible();
  await expect(page.locator(".synthetic-notice")).toContainText(
    "Exemplo sintético",
  );
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute(
    "href",
    /\/pt-BR\/research\/synthetic-efficient-attention$/,
  );
  await expect(page.locator('link[hreflang="en"]')).toHaveAttribute(
    "href",
    /\/en\/research\/synthetic-efficient-attention$/,
  );
  expect(
    await page.evaluate(
      () => document.documentElement.scrollWidth <= window.innerWidth,
    ),
  ).toBe(true);
});

test("missing publications and unsupported locales return a navigable 404", async ({
  page,
}) => {
  const missing = await page.goto("/en/research/does-not-exist");
  expect(missing?.status()).toBe(404);
  await expect(
    page.getByRole("heading", { name: "This research page isn’t available" }),
  ).toBeVisible();
  await page
    .getByRole("link", { name: "Back to research", exact: true })
    .click();
  await expect(page).toHaveURL(/\/en$/);
  const invalid = await page.goto("/fr");
  expect(invalid?.status()).toBe(404);
});

test("RSS, sitemap and robots expose coherent public routes", async ({
  request,
}) => {
  const rss = await request.get("/en/rss.xml");
  expect(rss.status()).toBe(200);
  expect(rss.headers()["content-type"]).toContain("application/rss+xml");
  expect(await rss.text()).toContain("[Synthetic example]");
  const sitemap = await request.get("/sitemap.xml");
  expect(sitemap.status()).toBe(200);
  expect(await sitemap.text()).toContain("/pt-BR</loc>");
  expect(await sitemap.text()).not.toContain("/research/synthetic-");
  const robots = await request.get("/robots.txt");
  expect(robots.status()).toBe(200);
  expect(await robots.text()).toContain("/sitemap.xml");
  if (isolated)
    expect(await robots.text()).toContain("http://127.0.0.1:3100/sitemap.xml");
});

test("upstream failure is explicit and retry recovers real API data", async ({
  page,
  request,
}) => {
  test.skip(
    !isolated,
    "Service fault injection is restricted to the isolated test API.",
  );
  await request.post("http://127.0.0.1:5109/__test/control?unavailable=true");
  await page.goto("/en");
  await expect(
    page.getByRole("heading", { name: "Research is temporarily unavailable" }),
  ).toBeVisible();
  await expect(page.locator(".research-card")).toHaveCount(0);
  expect((await request.get("/en/rss.xml")).status()).toBe(503);
  await request.post("http://127.0.0.1:5109/__test/control?unavailable=false");
  await page.getByRole("button", { name: "Try again" }).click();
  await expect(page.locator(".research-card")).toHaveCount(1);
});
