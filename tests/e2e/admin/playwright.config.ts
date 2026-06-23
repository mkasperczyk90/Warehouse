import path from 'node:path';

import { defineConfig, devices } from '@playwright/test';
import { defineBddConfig } from 'playwright-bdd';

/** The admin panel under test (Vite dev server, ADR-0004). */
const ADMIN_APP = path.join(__dirname, '..', '..', '..', 'src', 'web', 'admin');
const PORT = 5179;
const BASE_URL = `http://localhost:${PORT}`;

/**
 * Generate Playwright specs from the Gherkin features + step definitions.
 * `testDir` points at the generated `.features-gen` folder; run `bddgen`
 * (done automatically by the npm scripts) before `playwright test`.
 */
const testDir = defineBddConfig({
  features: 'e2e/features/**/*.feature',
  steps: ['e2e/steps/**/*.ts', 'fixtures/**/*.ts'],
});

export default defineConfig({
  testDir,
  // The first request triggers a Vite cold build + MSW worker boot, so be generous.
  timeout: 60_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  // On CI also emit GitHub annotations (inline pass/fail) + JUnit for the test reporter action.
  reporter: process.env.CI
    ? [['list'], ['github'], ['junit', { outputFile: 'test-results/junit.xml' }], ['html', { open: 'never' }]]
    : [['list'], ['html', { open: 'never' }]],

  use: {
    baseURL: BASE_URL,
    navigationTimeout: 30_000,
    actionTimeout: 15_000,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'admin-desk',
      use: {
        ...devices['Desktop Chrome'],
        // The admin is a desk app — test at a wide viewport so the sidebar shows.
        viewport: { width: 1440, height: 900 },
      },
    },
  ],

  // Boot the Vite dev server automatically and wait for it to answer.
  webServer: {
    command: `npm run dev -- --port ${PORT} --strictPort`,
    cwd: ADMIN_APP,
    url: BASE_URL,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    stdout: 'pipe',
    stderr: 'pipe',
  },
});
