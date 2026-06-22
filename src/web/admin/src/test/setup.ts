import '@testing-library/jest-dom/vitest';

import { afterAll, afterEach, beforeAll } from 'vitest';
import { cleanup } from '@testing-library/react';
import { setupServer } from 'msw/node';

import { handlers } from '@/core/mocks/handlers';

// We run with globals:false, so RTL's automatic cleanup isn't registered —
// unmount between tests explicitly to keep each test's DOM isolated.
afterEach(() => cleanup());

// Tests run against the SAME MSW handlers that back the dev server (ADR-0006) —
// fixtures are defined once and serve both. `error` on unhandled requests keeps
// a forgotten endpoint from silently passing.
export const server = setupServer(...handlers);

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
