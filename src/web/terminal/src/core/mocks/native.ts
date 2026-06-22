import { setupServer } from 'msw/native';

import { handlers } from './handlers';

/** The request interceptor for a real handheld (Expo Go / native). Same ADR-0006 seam as the web worker. */
export const server = setupServer(...handlers);
