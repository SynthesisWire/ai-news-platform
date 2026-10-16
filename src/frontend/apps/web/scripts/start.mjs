import { parseArgs } from "node:util";
const { values } = parseArgs({
  options: {
    port: { type: "string", short: "p" },
    hostname: { type: "string", short: "H" },
  },
});
if (values.port) process.env.PORT = values.port;
// HOSTNAME is commonly prepopulated with the machine name in containers.
process.env.HOSTNAME = values.hostname || "0.0.0.0";
await import("../.next/standalone/server.js");
