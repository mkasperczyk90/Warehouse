import { setupWorker } from 'msw/browser';

import { handlers } from './handlers';

/** The mock service worker. Started from main.tsx; the ADR-0006 on/off switch. */
export const worker = setupWorker(...handlers);
