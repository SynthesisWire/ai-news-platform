import { execFileSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { pathToFileURL } from "node:url";

function externalOrPlaceholder(value) {
  const candidate = value.trim().replace(/^["']|["']$/g, "");
  if (/\$\{[A-Za-z_][A-Za-z0-9_]*:-[^$}]+\}/.test(candidate)) return false;
  return !candidate || /^(?:\$\{|\$[A-Za-z_]|<[^>]+>$)/.test(candidate);
}

// Diagnostics deliberately contain no matched content or candidate values.
export function inspectCredentials(path, content) {
  const findings = [];
  if (/(?:^|\/)\.env(?:\..+)?$/.test(path) && !path.endsWith(".env.example"))
    findings.push({ path, line: 1, reason: "tracked environment file" });
  content.split(/\r?\n/).forEach((line, index) => {
    const candidates = [
      ...line.matchAll(/(?:^|[;\s"'`])(?:password|pwd)\s*=\s*([^;\r\n"'`]+)/gi),
      ...line.matchAll(/^\s*["']?(?:[A-Z][A-Z0-9_]*_)?(?:PASSWORD|PASSWD|PWD)["']?\s*[:=]\s*(.+)$/gi),
      ...line.matchAll(/\b(?:password|passwd|pwd)\s*[:=]\s*["']([^"'\r\n]+)["']/gi),
      ...line.matchAll(/[a-z][a-z0-9+.-]*:\/\/[^/\s:@]+:([^@\s/]+)@/gi),
    ];
    if (candidates.some(match => !externalOrPlaceholder(match[1])))
      findings.push({ path, line: index + 1, reason: "password literal or credential-bearing URL" });
    if (/-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----|\b(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|AKIA[A-Z0-9]{16})\b/.test(line))
      findings.push({ path, line: index + 1, reason: "potential private key or access credential" });
  });
  return findings;
}

export function inspectTrackedTree() {
  const paths = execFileSync("git", ["ls-files", "-z"], { encoding: "utf8" }).split("\0").filter(Boolean);
  return paths.flatMap(path => {
    const bytes = readFileSync(path);
    return bytes.includes(0) ? [] : inspectCredentials(path, bytes.toString("utf8"));
  });
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  const findings = inspectTrackedTree();
  for (const finding of findings)
    console.error(`${finding.path}:${finding.line}: ${finding.reason}`);
  console.log(`Credential policy: ${findings.length} findings.`);
  process.exitCode = findings.length ? 1 : 0;
}
