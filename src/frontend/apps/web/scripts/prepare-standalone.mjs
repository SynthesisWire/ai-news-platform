import { cp } from "node:fs/promises";
// Next's standalone output deliberately excludes static assets. Package them for
// a self-contained local runner and the production container.
await cp(".next/static", ".next/standalone/.next/static", { recursive: true });
await cp("public", ".next/standalone/public", { recursive: true });
