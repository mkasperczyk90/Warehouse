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
  },
});
