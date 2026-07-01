import path from 'node:path';

import { defineConfig, devices, type PlaywrightTestConfig } from '@playwright/test';

/**
 * Demo recorder — drives the `docs/demo-walkthrough.md` golden path across both
 * front ends and records a video (later turned into a GIF by `make-gif.mjs`),
 * with a visible, animated mouse cursor and on-screen scene captions.
 *
 * The apps run from the **main checkout** (not this folder), so the recording
 * always exercises the current working code. Point `DEMO_APP_ROOT` at the repo
 * root that holds `src/web/*`; it defaults to two levels up (correct once this
 * folder lives at `<repo>/scripts/demo-recording`).
 */
const APP_ROOT = process.env.DEMO_APP_ROOT
  ? path.resolve(process.env.DEMO_APP_ROOT)
  : path.resolve(__dirname, '..', '..');

const ADMIN_APP = path.join(APP_ROOT, 'src', 'web', 'admin');
const TERMINAL_APP = path.join(APP_ROOT, 'src', 'web', 'terminal');

const ADMIN_PORT = 5179;
const TERMINAL_PORT = 8081;
const ADMIN_URL = `http://localhost:${ADMIN_PORT}`;
const TERMINAL_URL = `http://localhost:${TERMINAL_PORT}`;

// `DEMO_ONLY=admin|terminal` boots just one app (the Expo bundle is slow, so a
// fast admin-only run shouldn't pay for it).
const ONLY = process.env.DEMO_ONLY;

const adminServer: NonNullable<PlaywrightTestConfig['webServer']> = {
  // MSW must be initialised once (writes public/mockServiceWorker.js, gitignored).
  command: `npm run mock:init && npm run dev -- --port ${ADMIN_PORT} --strictPort`,
  cwd: ADMIN_APP,
  url: ADMIN_URL,
  reuseExistingServer: true,
  timeout: 180_000,
  stdout: 'pipe',
  stderr: 'pipe',
};

const terminalServer: NonNullable<PlaywrightTestConfig['webServer']> = {
  command: `npm run web -- --port ${TERMINAL_PORT}`,
  cwd: TERMINAL_APP,
  url: TERMINAL_URL,
  reuseExistingServer: true,
  // First request triggers a Metro bundle — be generous.
  timeout: 300_000,
  stdout: 'pipe',
  stderr: 'pipe',
  env: { BROWSER: 'none' },
};

const webServer = [
  ...(ONLY === 'terminal' ? [] : [adminServer]),
  ...(ONLY === 'admin' ? [] : [terminalServer]),
];

export default defineConfig({
  testDir: './specs',
  // The golden path is long — one test walks ~7 scenes.
  timeout: 600_000,
  expect: { timeout: 20_000 },
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list']],
  // Keep the webm even on success — that's the whole point.
  preserveOutput: 'always',
  outputDir: './test-results',
  // Turn the recordings into GIFs + MP4s once the take(s) finish.
  globalTeardown: './globalTeardown.ts',

  use: {
    actionTimeout: 20_000,
    navigationTimeout: 60_000,
    trace: 'off',
    screenshot: 'off',
    // Slow the synthetic input a touch so the cursor reads as human.
    launchOptions: { slowMo: Number(process.env.DEMO_SLOWMO ?? '120') },
  },

  projects: [
    {
      name: 'admin-desk',
      testMatch: /admin\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        baseURL: ADMIN_URL,
        viewport: { width: 1440, height: 900 },
        video: { mode: 'on', size: { width: 1440, height: 900 } },
      },
    },
    {
      name: 'terminal-handheld',
      testMatch: /terminal\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        baseURL: TERMINAL_URL,
        // The terminal is designed at 390 px — record at the handheld width.
        viewport: { width: 390, height: 844 },
        video: { mode: 'on', size: { width: 390, height: 844 } },
      },
    },
  ],

  webServer,
});
