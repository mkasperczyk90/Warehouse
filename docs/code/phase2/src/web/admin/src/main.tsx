import './shared/theme/tokens.css';
import './shared/theme/global.css';
import './shared/i18n/i18n';

import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from '@tanstack/react-router';

import { queryClient } from './core/api/queryClient';
import { router } from './router';

/**
 * Until the Gateway is wired up, the admin runs against MSW fixtures (ADR-0006).
 * Going live is making this a no-op (or gating it on an env flag) — the `fetch`
 * calls and the components stay exactly as they are.
 */
async function enableMocking() {
  const { worker } = await import('./core/mocks/browser');
  await worker.start({ onUnhandledRequest: 'bypass' });
}

void enableMocking().then(() => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
      </QueryClientProvider>
    </StrictMode>,
  );
});
