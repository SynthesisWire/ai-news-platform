# SynthesisWire / Editorial Intelligence

The Next.js App Router public web for SynthesisWire, with the chosen tagline
Editorial Intelligence. This identity is preserved in English and Brazilian Portuguese.
This is an executable foundation, not a production editorial or ingestion system.

## Run locally

Requires Node.js 24+ and the API running on `http://localhost:5080`.

```sh
cp .env.example .env.local
npm ci
npm run dev
```

Open `http://localhost:3000/pt-BR` or `/en`. In restricted environments, set
`npm_config_cache=/tmp/npm-cache` when the default npm home is not writable.

The server-only `API_BASE_URL` selects the ASP.NET Core API. `SITE_URL` is the
public origin used for canonical URLs, alternate languages, RSS and the sitemap.
No account, hosted service, external font, API key or paid service is required.

## Production build and local runner

```sh
npm run build
API_BASE_URL=http://localhost:5080 SITE_URL=http://localhost:3000 npm start
# Optional custom port:
PORT=3100 SITE_URL=http://localhost:3100 npm start
```

`postbuild` copies public/static assets into the standalone output. `npm start`
launches that standalone server (also accepts `-- --port 3100`). The Dockerfile
uses a non-root Node 24 runtime; its build context is this directory. Provide
`API_BASE_URL` and `SITE_URL` at container runtime. No API is required at build time.

## Implemented

- Responsive English and Brazilian Portuguese research feeds and detail pages
- Search and topic filtering over the returned publication list
- Canonical claims, linked evidence, precise source locators, limitations,
  deduplicated sources and visible publication history
- Source-language labels for canonical claims and quoted evidence; localized
  summaries are never presented as independently regenerated canonical records
- Explicit synthetic-demo labels, including in RSS; synthetic details are
  `noindex` and excluded from sitemap discovery
- Per-locale canonical URLs and only verified published article hreflang links
- `/en/rss.xml`, `/pt-BR/rss.xml`, `/sitemap.xml` and runtime-origin `/robots.txt`
- API response validation, safe source protocols, bounded fetch timeout, real
  missing-record 404s, retryable service errors, and a 503 RSS failure response
- Keyboard focus states, skip navigation, document language, reduced-motion CSS,
  readable mobile layouts and no required third-party scripts

Production content is fetched exclusively from the API. Test fixtures live under
`tests/` and are never used as an application fallback. This frontend has no
Notion access, editorial writes, paid integrations or private data.

## Verification

```sh
npm run typecheck
npm test
npm run build
npm audit --omit=dev
npx playwright install --with-deps chromium
npm run test:e2e
```

By default E2E tests run against the production build with a **test-only** API
fixture on `127.0.0.1:5109` and the web on `127.0.0.1:3100`. They cover desktop
and mobile reading, filtering, language switching, metadata, real 404s, RSS,
sitemap, robots, upstream outage and retry. The isolated server is not shipped.

To test a running real API + web stack seeded with the synthetic demo:

```sh
E2E_BASE_URL=http://127.0.0.1:3000 npm run test:e2e
```

An externally supplied URL disables server startup and destructive test fault
injection. To use a system Chromium, set
`PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH=/usr/bin/chromium`. Otherwise Playwright uses
its installed browser. Screenshots and traces go to ignored `test-results/`.

## Dependencies and scope

Exact runtime versions were checked against the npm registry on October 16, 2026:
Next.js 16.4.2 and React/React DOM 19.3.1. Next.js 16.4.2 is newer than the
[October 14 patched 16.4.1 baseline](https://nextjs.org/blog/nextjs-security-update-october-14-2026).
The lockfile is committed for reproducible installs. See
[official Next.js installation](https://nextjs.org/docs/app/getting-started/installation).

The public list has client-side filtering of the API results, not backend full-text
search or pagination. Real evidence verification, automated ingestion, editorial
workflow, translation generation, distribution, production observability, deployed
cache policies and production access controls remain backend/product follow-up work.
The source data remains untrusted text; no source-provided HTML is executed.

For environments where a browser cannot start, `npm run test:e2e:http` runs only
HTTP-level Playwright assertions against the same production server. These verify
server-rendered localized records, metadata, 404s and feed routes; they do not
establish browser interaction, accessibility or visual correctness. It also honors
`E2E_BASE_URL` for the real API + web stack.
