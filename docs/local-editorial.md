# Local source ingestion and human editorial workflow

This is a local administrative vertical slice, not a hosted editorial service. The public API stays read-only. It creates no application accounts, persistent credentials, access grants or external integrations. The OS process owner and an externally supplied database connection are the trust boundary. Run only on a trusted local workstation/test environment against its development database. Do not offer this command through an HTTP shell, job submission endpoint or untrusted user account. Hosted identity, separate reviewer roles and production operations need a separate design decision.

## Setup and authority

Follow the README to supply the database secret at runtime, start PostgreSQL and set `ConnectionStrings__Postgres` privately. Existing 001 foundation databases migrate forward without resetting data:

```sh
dotnet run --project src/backend/SynthesisWire.Api -- --migrate
mkdir -p .editorial
export INGEST_ALLOWED_HOSTS=example.org
```

`.editorial/` is ignored. Keep requests, unpublished drafts and snapshot exports there or outside the repository. Do not print connection strings. `INGEST_ALLOWED_HOSTS` contains comma-separated exact DNS hostnames you are authorized to capture; it is empty by default. Wildcards are not supported. Allowing a host is a local scope restriction, not a claim of reuse permission.

## Capture, inspect and write

Create `.editorial/request.json`:

```json
{"action":"ingest","url":"https://example.org/"}
```

Run each request as a separate administrative command:

```sh
dotnet run --project src/backend/SynthesisWire.Api -- --editorial .editorial/request.json
```

The result supplies `id` (draft ID), `captureId`, `researchItemId`, `revision` and `state`. IDs are generated UUID v7. An exact URL/content retry returns the existing draft. A changed source gets a new immutable capture; similar whitespace-normalized bytes at another URL produce `possibleDuplicateCaptureId` for human reconciliation, never an automatic merge. Semantic equivalence, boilerplate removal and substantive-change classification are not implemented.

Inspect with `{"action":"show","draftId":"REPLACE-WITH-RETURNED-DRAFT-ID"}`. Output is JSON, with untrusted HTML/control characters escaped. Never render `sourceText` as HTML or follow instructions embedded in it. Snapshots retain requested/final URL, fetch time, media type, original bytes and SHA-256; the public API never serves these bytes. Internal snapshots are bounded private PostgreSQL records, not a public article archive. No HTML assets, scripts, browser navigation, subresources, login pages or provider tools are executed. The fetch timestamp is not the source publication date.

Supply all structured content yourself. This template is not news and must be replaced by an authorized human draft; it is deliberately incomplete until its identifiers, dates, evidence and rights are verified:

```json
{
  "action": "save",
  "draftId": "REPLACE-WITH-RETURNED-DRAFT-ID",
  "expectedRevision": 0,
  "content": {
    "slug": "human-reviewed-source",
    "canonicalTitle": "Human-authored canonical title",
    "sourceTitle": "Verified source title",
    "publisher": "Verified source publisher",
    "sourcePublishedAt": "REPLACE-WITH-VERIFIED-UTC-SOURCE-DATE",
    "rightsConfirmed": false,
    "claims": [{"statement":"Human-attributed claim, not an independent verification", "excerpt":"A short verbatim excerpt actually present in the capture", "locator":"Verified source paragraph or section"}],
    "english": {"title":"Human-written English title", "summary":"Human-written English summary", "whyItMatters":"Human assessment", "limitations":["Verified limitations"]},
    "portuguese": {"title":"Título escrito por pessoa", "summary":"Resumo escrito por pessoa", "whyItMatters":"Avaliação humana", "limitations":["Limitações verificadas"]}
  }
}
```

Set `rightsConfirmed` to true only after checking permission/license and permitted excerpts. Length limits are not copyright permission: each excerpt is at most 280 characters, total excerpts at most 560, and a human must ensure lawful quotation and original summaries. The software verifies that evidence excerpts occur verbatim in the captured UTF-8 source; this does not verify the truth, context, publisher identity, date or translation. Claims remain canonical human-authored attributed text (source language); localized titles/summaries/limitations are independently human-written. There is no automatic translation or generated fact. Real captures are not marked synthetic; deterministic tests use explicitly synthetic fixtures.

## Submit, review and publish

Use the latest returned revision in every mutation. A stale revision fails instead of overwriting work.

1. `{"action":"submit","draftId":"...","expectedRevision":1}` moves Draft to InReview after validation.
2. Inspect the source, claims, both languages, limitations and rights. Then `{"action":"review","draftId":"...","expectedRevision":1,"confirmed":true}` records explicit human review.
3. `{"action":"publish","draftId":"...","expectedRevision":1}` publishes both locales in one transaction, including canonical source/evidence, immutable publication snapshots, outbox events and the editorial audit event. Repeating or concurrently retrying this request does not duplicate publication/outbox records.

`revise` returns an InReview/Reviewed draft to Draft, increments its revision and clears approval. Save again, submit and review the new revision before publishing. Published drafts cannot be edited by these commands; post-publication correction commands are not part of this increment. The earlier immutable version-history mechanism remains intact.

The recorded actor is the OS username, not a JSON-supplied identity. A single trusted local operator may author and review in separate explicit steps; this is not two-person separation of duties. Events and captures reject SQL UPDATE/DELETE. Draft transitions lock the row; publication, state transition and audit commit together or roll back together. Published content appears through the existing `/en` and `/pt-BR` lists/detail pages, localized RSS, sitemap and language alternates. Raw source bytes and draft data do not enter public reads. Do not execute a real-source publication as a smoke test or expose this development stack publicly.

## Fetch security and intentional limits

- HTTPS/443 only, exact allowed DNS hostnames, no credentials, query strings, fragments or IP-literal URLs; no HTTP downgrade. Each redirect is revalidated, with at most three redirects.
- Actual DNS resolution happens inside `SocketsHttpHandler.ConnectCallback`; all returned addresses must be public. The socket uses a checked IPv4 address directly, preventing a second DNS lookup/rebinding. IPv6-only destinations are unsupported and fail closed. IPv4 private/link-local/loopback/shared/documentation/multicast ranges and IPv6 local/transition/documentation addresses are blocked.
- System proxies, cookies, automatic credentials, automatic decompression and auto-redirects are disabled. Normal platform TLS certificate/hostname validation remains enabled. No certificate bypass or trusted-root change is installed.
- Twenty-second total deadline, five-second connect timeout, 16 KiB response-header limit, 512 KiB body limit enforced while streaming even without Content-Length. Only uncompressed UTF-8 text/plain/text/html is supported; unsupported media/charset, empty or malformed captures fail without persistence.
- No crawler, feed polling, JavaScript rendering, authenticated/paywalled source, PDF processing, provider generation, hosted editorial UI, multi-user authorization, production deploy or material semantic deduplication is included. Rate is operator-driven; no background fetch or external automation is created.

Security references: [OWASP SSRF prevention](https://cheatsheetseries.owasp.org/cheatsheets/Server_Side_Request_Forgery_Prevention_Cheat_Sheet.html), [Microsoft ConnectCallback](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.socketshttphandler.connectcallback?view=net-10.0).

## Verification

CI exercises deterministic synthetic HTTP responses, dangerous URL/address/redirect cases, time/size/media limits, real PostgreSQL capture/review/publication concurrency, immutable capture/audit records, evidence/rights rejection and unchanged public contracts. Existing real API/Next smoke, RSS, browser and outage/retry suites verify the public projections. No public site is deployed by CI.

An optional real-network test captures only the public example.org example document, retains it in memory for the test and never publishes or writes a database record:

```sh
SYNTHESISWIRE_PUBLIC_CAPTURE_SMOKE=true dotnet test --filter Optional_real_public_capture_smoke
```

It is deliberately skipped in deterministic CI and must be reported separately as passed, failed or not run. The ordinary database tests require the externally supplied disposable test connection, as documented in development.md.
