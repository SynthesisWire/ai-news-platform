import assert from "node:assert/strict";
import { execFileSync, spawnSync } from "node:child_process";
import { mkdtempSync, writeFileSync, rmSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

// Run after the ordinary browser suites, against the disposable CI stack only.
assert.equal(process.env.GITHUB_ACTIONS, "true");
assert.equal(process.env.SYNTHESISWIRE_EDITORIAL_E2E, "true");
assert.ok(process.env.TEST_POSTGRES_CONNECTION_STRING);
assert.equal(process.env.ConnectionStrings__Postgres, process.env.TEST_POSTGRES_CONNECTION_STRING);
const api = new URL(process.env.API_BASE_URL ?? "http://127.0.0.1:5080");
const web = new URL(process.env.WEB_BASE_URL ?? "http://127.0.0.1:3000");
for (const origin of [api, web]) {
  assert.ok(["localhost", "127.0.0.1"].includes(origin.hostname), "Only local test services may be targeted");
  assert.equal(origin.username + origin.password, "");
}
const apiProject = "src/backend/SynthesisWire.Api";
const runArgs = (project) => ["run", "--project", project, "--configuration", "Release", "--no-build", "--"];
const parseOutput = (output) => JSON.parse(output.trim().split(/\r?\n/).at(-1));
const folder = mkdtempSync(join(tmpdir(), "synthesiswire-editorial-e2e-"));
try {
  // The only injected dependency lives in the test executable; the real CLI keeps its safe fetcher.
  const draft = parseOutput(execFileSync("dotnet", runArgs("tests/tools/SynthesisWire.EditorialFixture"), { encoding: "utf8", timeout: 30_000 }));
  const slug = `synthetic-cli-${draft.id}`;
  assert.equal(draft.state, "Draft");
  const input = join(folder, "request.json");
  const cli = (request, succeeds = true) => {
    writeFileSync(input, JSON.stringify({ draftId: draft.id, ...request }), { mode: 0o600 });
    const result = spawnSync("dotnet", [...runArgs(apiProject), "--editorial", input], { encoding: "utf8", timeout: 30_000 });
    assert.ifError(result.error);
    if (!succeeds) {
      assert.notEqual(result.status, 0, "The real CLI must reject this transition");
      return;
    }
    // Do not print stderr, arguments or environment on failure: they may contain local diagnostics.
    assert.equal(result.status, 0, "Editorial CLI exited unsuccessfully");
    return parseOutput(result.stdout);
  };
  const get = (base, path) => fetch(new URL(path, base), { signal: AbortSignal.timeout(15_000) });
  const absent = async () => {
    for (const locale of ["en", "pt-BR"]) {
      assert.equal((await get(api, `/api/research-items/${slug}?locale=${locale}`)).status, 404);
      assert.equal((await get(web, `/${locale}/research/${slug}`)).status, 404);
      assert.ok(!(await (await get(web, `/${locale}/rss.xml`)).text()).includes(slug));
    }
  };
  const titles = { en: "Synthetic CLI publication in English", "pt-BR": "Publicação sintética pelo CLI em português" };
  const content = {
    slug, canonicalTitle: "Synthetic CLI canonical record", sourceTitle: "Synthetic CLI fixture",
    publisher: "Fictional test publisher", sourcePublishedAt: "2020-01-01T00:00:00Z", rightsConfirmed: true,
    claims: [{ statement: "This is synthetic test evidence, not a factual finding.", excerpt: "Synthetic CLI evidence only.", locator: "Synthetic fixture paragraph 1" }],
    english: { title: titles.en, summary: "Synthetic summary authored for the CLI test.", whyItMatters: "Tests the supported CLI and live projections.", limitations: ["Synthetic test only."] },
    portuguese: { title: titles["pt-BR"], summary: "Resumo sintético escrito para o teste do CLI.", whyItMatters: "Verifica o CLI e as projeções reais.", limitations: ["Somente teste sintético."] },
  };
  await absent();
  const shown = cli({ action: "show" });
  assert.equal(shown.sourceText, "Synthetic CLI evidence only. No real research finding.");
  assert.equal(cli({ action: "save", content }).revision, 1);
  cli({ action: "publish", expectedRevision: 1 }, false);
  assert.equal(cli({ action: "submit", expectedRevision: 1 }).state, "InReview");
  cli({ action: "review", expectedRevision: 1 }, false);
  await absent();
  assert.equal(cli({ action: "review", expectedRevision: 1, confirmed: true }).state, "Reviewed");
  await absent();
  assert.equal(cli({ action: "publish", expectedRevision: 1 }).state, "Published");
  assert.equal(cli({ action: "publish", expectedRevision: 1 }).state, "Published");
  for (const locale of ["en", "pt-BR"]) {
    const response = await get(api, `/api/research-items/${slug}?locale=${locale}`);
    assert.equal(response.status, 200);
    const item = await response.json();
    assert.equal(item.id, draft.researchItemId);
    assert.equal(item.title, titles[locale]);
    assert.equal(item.isSynthetic, true);
    assert.equal(item.versions.length, 1, "CLI retry must not duplicate publication");
    assert.equal(item.claims[0].evidence[0].excerpt, content.claims[0].excerpt);
    const list = await (await get(api, `/api/research-items?locale=${locale}`)).json();
    assert.equal(list.items.filter((entry) => entry.id === item.id).length, 1);
    for (const path of [`/${locale}`, `/${locale}/research/${slug}`]) {
      const page = await get(web, path);
      assert.equal(page.status, 200);
      const html = await page.text();
      assert.ok(html.includes(titles[locale]), `${path} renders the new CLI publication`);
      assert.ok(html.includes(`lang="${locale}"`));
    }
    const feed = await get(web, `/${locale}/rss.xml`);
    assert.equal(feed.status, 200);
    const xml = await feed.text();
    assert.ok(xml.includes(titles[locale]));
    assert.ok(xml.includes(`/${locale}/research/${slug}`));
    assert.equal((xml.match(new RegExp(`<guid isPermaLink="true">[^<]*${slug}</guid>`, "g")) ?? []).length, 1);
  }
  console.log("Editorial CLI E2E passed: synthetic capture setup; real CLI save/submit/review/publish/retry; draft privacy; new canonical API item, PT-BR/EN pages and RSS.");
} finally {
  rmSync(folder, { recursive: true, force: true });
}
