import { test } from "node:test";
import assert from "node:assert/strict";
import { randomBytes } from "node:crypto";
import { readFileSync } from "node:fs";
import { inspectCredentials } from "./check-credentials.mjs";

test("rejects password literals across config, connection strings, code, docs, and URLs without leaking them", () => {
  const candidate = randomBytes(32).toString("hex");
  const assignment = (key, separator) => `${key}${separator}${candidate}`;
  for (const content of [
    assignment("POSTGRES_PASSWORD", "="),
    assignment("  POSTGRES_PASSWORD", ": "),
    `"Password": "${candidate}"`,
    `Host=localhost;${assignment("Password", "=")};Database=test`,
    `const password = "${candidate}";`,
    `https://user:${candidate}@example.org`,
    `postgresql://user:${candidate}@localhost/test`,
    ["POSTGRES_PASSWORD", ["${POSTGRES_PASSWORD", candidate + "}"].join(":-")].join(": "),
  ]) {
    const findings = inspectCredentials("fixture", content);
    assert.ok(findings.length > 0);
    assert.ok(!JSON.stringify(findings).includes(candidate));
  }
});

test("permits empty examples, explicit placeholders and runtime secret references", () => {
  const assignment = (key, value) => [key, value].join("=");
  for (const content of [
    assignment("POSTGRES_PASSWORD", ""),
    "POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?Supply a unique local secret}",
    assignment("Password", "${secret}"),
    assignment("Password", "$DATABASE_SECRET"),
    assignment("Password", "<supplied-at-runtime>"),
    "if (url.password) reject();",
    "url.password = randomBytes(32).toString('hex');",
  ]) assert.deepEqual(inspectCredentials(".env.example", content), []);
});

test("rejects tracked environment files except examples", () => {
  for (const path of [".env", ".env.local", "src/web/.env.production"])
    assert.equal(inspectCredentials(path, "")[0].reason, "tracked environment file");
  assert.deepEqual(inspectCredentials("src/web/.env.example", ""), []);
});

test("Compose requires a supplied password everywhere and CI starts databases after generation", () => {
  const compose = readFileSync("docker-compose.yml", "utf8");
  assert.equal((compose.match(/\$\{POSTGRES_PASSWORD:\?/g) || []).length, 3);
  assert.ok(!compose.includes("POSTGRES_PASSWORD:-"));
  const workflow = readFileSync(".github/workflows/ci.yml", "utf8");
  assert.ok(!/^    services:/m.test(workflow));
  assert.equal((workflow.match(/run: node scripts\/ci-secrets.mjs/g) || []).length, 2);
  assert.ok(workflow.indexOf("run: node scripts/ci-secrets.mjs") < workflow.indexOf("run: docker compose up"));
  const api = readFileSync("src/backend/SynthesisWire.Api/Program.cs", "utf8");
  assert.ok(api.includes("string.IsNullOrWhiteSpace(connection)"));
});
