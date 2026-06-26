import './shared/theme/tokens.css';
import './shared/theme/global.css';
import './shared/i18n/i18n';

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';

import { queryClient } from './core/api/queryClient';
import { App } from './App';

/**
 * MSW fixtures are the default (ADR-0006), so `npm run dev`, the tests and the standalone preview
 * image keep working untouched. Build with `VITE_USE_MOCKS=false` to turn the worker off and let the
 * same `fetch` calls hit the real Gateway (proxied at /api by nginx). It is a build-time flag — these
 * are static bundles, so it must be set when Vite builds, not at runtime.
 */
const USE_MOCKS = import.meta.env.VITE_USE_MOCKS !== 'false';

async function enableMocking() {
  if (!USE_MOCKS) return;
  const { worker } = await import('./core/mocks/browser');
  await worker.start({ onUnhandledRequest: 'bypass' });
}

void enableMocking().then(() => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <App />
      </QueryClientProvider>
    </StrictMode>,
  );
});
