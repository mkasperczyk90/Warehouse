import { setupWorker } from 'msw/browser';

import { handlers } from './handlers';

/** The mock service worker (web / Expo web). The ADR-0006 on/off switch. */
export const worker = setupWorker(...handlers);
