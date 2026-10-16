// This server exists solely for isolated browser tests. It is never shipped as app data.
import { createServer } from "node:http";
import { fixture } from "../fixture.ts";
let unavailable = false;
createServer((request, response) => {
  const url = new URL(request.url, "http://127.0.0.1:5109");
  response.setHeader("Content-Type", "application/json");
  if (url.pathname === "/health") return response.end('{"status":"ok"}');
  if (url.pathname === "/__test/control" && request.method === "POST") {
    unavailable = url.searchParams.get("unavailable") === "true";
    return response.end('{"status":"ok"}');
  }
  if (unavailable) {
    response.statusCode = 503;
    return response.end('{"error":"Test-only service unavailable"}');
  }
  const locale = url.searchParams.get("locale");
  if (locale !== "en" && locale !== "pt-BR") {
    response.statusCode = 400;
    return response.end("{}");
  }
  if (url.pathname === "/api/research-items")
    return response.end(JSON.stringify({ items: [fixture(locale)] }));
  if (url.pathname === "/api/research-items/synthetic-efficient-attention")
    return response.end(JSON.stringify(fixture(locale)));
  response.statusCode = 404;
  response.end("{}");
}).listen(5109, "127.0.0.1");
