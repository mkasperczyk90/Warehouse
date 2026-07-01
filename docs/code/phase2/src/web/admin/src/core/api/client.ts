/**
 * The single seam to the backend (the .NET Gateway).
 *
 * Unlike the terminal's synchronous fixture mock, the admin calls `fetch` from
 * day one (ADR-0006). In dev/test MSW intercepts these requests at the network
 * layer and returns fixtures; in production MSW is off and the same calls hit
 * the real Gateway. Going live is turning MSW off — not rewriting components.
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
    throw new ApiError(res.status, code, payload?.message ?? res.statusText);
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
