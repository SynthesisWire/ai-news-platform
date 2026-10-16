import { randomBytes } from "node:crypto";
import { appendFileSync } from "node:fs";

if (process.env.GITHUB_ACTIONS !== "true" || !process.env.GITHUB_ENV)
  throw new Error("This helper requires a GitHub Actions job environment.");

const secret = randomBytes(32).toString("hex");
// Mask before making the ephemeral value available to subsequent steps.
console.log(`::add-mask::${secret}`);
const connection = `Host=localhost;Port=5432;Database=airesearch;Username=airesearch;Password=${secret}`;
appendFileSync(process.env.GITHUB_ENV, [
  `POSTGRES_PASSWORD=${secret}`,
  `ConnectionStrings__Postgres=${connection}`,
  `TEST_POSTGRES_CONNECTION_STRING=${connection}`,
  "",
].join("\n"));
