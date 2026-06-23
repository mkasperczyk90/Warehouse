import path from 'node:path';

import { defineConfig, devices } from '@playwright/test';
import { defineBddConfig } from 'playwright-bdd';

/** The Operator terminal under test (Expo web dev server). */
const TERMINAL_APP = path.join(__dirname, '..', '..', '..', 'src', 'web', 'terminal');
const BASE_URL = 'http://localhost:8081';

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
  // The first request triggers a Metro bundle (~10s), so be generous.
  timeout: 120_000,
  expect: { timeout: 15_000 },
  fullyParallel: false,
  workers: 1,
  retries: process.env.CI ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],

  use: {
    baseURL: BASE_URL,
    navigationTimeout: 60_000,
    actionTimeout: 15_000,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'terminal-handheld',
      use: {
        ...devices['Desktop Chrome'],
        // The terminal is designed at 390 px — test at the handheld width.
        viewport: { width: 390, height: 844 },
      },
    },
  ],

  // Boot the Expo web dev server automatically and wait for it to answer.
  webServer: {
    command: 'npm run web -- --port 8081',
    cwd: TERMINAL_APP,
    url: BASE_URL,
    reuseExistingServer: !process.env.CI,
    timeout: 240_000,
    stdout: 'pipe',
    stderr: 'pipe',
    env: { BROWSER: 'none' },
  },
});
