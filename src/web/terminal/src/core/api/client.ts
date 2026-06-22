/**
 * The single seam to the backend (the .NET Gateway).
 *
 * Like the admin panel (ADR-0006), the terminal calls `fetch` from day one. In
 * dev/test MSW intercepts these requests — on web through a Service Worker
 * (`mocks/browser`), on a real handheld through a request interceptor
 * (`mocks/native`) — and returns fixtures. In production MSW is off and the same
 * calls hit the real Gateway, so going live is turning the worker off, never a
 * rewrite. Every feature's data access flows through `api.get/post`.
 */

/** Base path for the Gateway (YARP reverse proxy). */
export const GATEWAY = '/api';

class ApiError extends Error {
  constructor(
    readonly status: number,
    /** Stable domain error code (echoes `DomainException`), when present. */
    readonly code: string | undefined,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export { ApiError };

async function request<T>(method: string, resource: string, body?: unknown): Promise<T> {
  const res = await fetch(`${GATEWAY}/${resource}`, {
    method,
    headers: body !== undefined ? { 'Content-Type': 'application/json' } : undefined,
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });

  if (!res.ok) {
    // Surface failures by their stable code (the same language the API returns).
    const payload = await res.json().catch(() => null);
    const code = payload && typeof payload === 'object' ? (payload as { code?: string }).code : undefined;
    const message =
      (payload && typeof payload === 'object' && (payload as { message?: string }).message) || res.statusText;
    throw new ApiError(res.status, code, message);
  }

  // 204 No Content — common for commands.
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export const api = {
  get: <T>(resource: string) => request<T>('GET', resource),
  post: <T>(resource: string, body?: unknown) => request<T>('POST', resource, body),
  put: <T>(resource: string, body?: unknown) => request<T>('PUT', resource, body),
};
