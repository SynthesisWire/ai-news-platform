import { defineConfig, devices } from "@playwright/test";

const externalUrl = process.env.E2E_BASE_URL;
const baseURL = externalUrl || "http://127.0.0.1:3100";
export default defineConfig({
  testDir: "./tests/e2e",
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? "github" : "list",
  use: {
    baseURL,
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
    launchOptions: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH
      ? { executablePath: process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH }
      : undefined,
  },
  projects: [
    {
      name: "desktop-chromium",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 1440, height: 1000 },
      },
    },
    {
      name: "mobile-chromium",
      use: { ...devices["iPhone 13"], defaultBrowserType: "chromium" },
    },
  ],
  webServer: externalUrl
    ? undefined
    : [
        {
          command: "node tests/e2e/fixture-api.mjs",
          url: "http://127.0.0.1:5109/health",
          reuseExistingServer: false,
        },
        {
          command: "npm start -- --port 3100",
          url: baseURL,
          reuseExistingServer: false,
          timeout: 60_000,
          env: {
            API_BASE_URL: "http://127.0.0.1:5109",
            SITE_URL: baseURL,
            NEXT_TELEMETRY_DISABLED: "1",
          },
        },
      ],
});
