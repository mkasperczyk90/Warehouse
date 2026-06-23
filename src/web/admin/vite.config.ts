/// <reference types="vitest/config" />
import { fileURLToPath, URL } from 'node:url';

import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// The admin panel is a plain browser SPA (ADR-0004). `@/*` mirrors the
// terminal's alias so the two front ends read alike; it maps to `src/*`.
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    // CSS Modules resolve to undefined class names in tests; we assert on
    // text/roles, not styles, so skipping CSS keeps runs fast.
    css: false,
    // CI runners are slower (shared ~2 vCPU); these async, MSW-backed component tests can
    // brush past the default 5s timeout under load. Give CI headroom and retry to absorb the
    // occasional resource-contention flake. Local runs keep the strict defaults.
    testTimeout: process.env.CI ? 20_000 : 5_000,
    retry: process.env.CI ? 2 : 0,
  },
});
