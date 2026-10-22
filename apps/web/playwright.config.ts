import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: "./tests",
  testMatch: "**/*.spec.ts",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [["list"], ["html", { open: "never" }]],
  use: {
    baseURL: process.env.SG_WEB_URL ?? "http://127.0.0.1:5080",
    viewport: { width: 1440, height: 1000 },
    screenshot: "only-on-failure",
    trace: "retain-on-failure",
    launchOptions: {
      executablePath:
        process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH === "managed"
          ? undefined
          : process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH ||
            "/usr/bin/chromium",
      args: ["--no-sandbox"],
    },
  },
});
