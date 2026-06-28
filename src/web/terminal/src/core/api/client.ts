/**
 * The single seam to the backend (the .NET Gateway).
 *
 * Like the admin panel (ADR-0006), the terminal calls `fetch` and always talks to
 * the real Gateway (proxied at `/api` by nginx). The handheld badges in to get a
 * Keycloak token, then carries it (plus the active warehouse) on every request so
 * the gateway can authorise and scope the data. Every feature's data access flows
 * through `api.get/post`.
 */

/** Base path for the Gateway (YARP reverse proxy). */
export const GATEWAY = '/api';

/** Header the Gateway reads to scope every request to the active warehouse. */
export const WAREHOUSE_HEADER = 'X-Warehouse-Id';

/**
 * The warehouse the handheld is working in. The terminal has no switcher — it is
 * set once at sign-in from the operator's home warehouse (see `AuthContext`) so
 * every request below carries it and the backend scopes the data.
 */
let activeWarehouseId: string | null = null;
export function setActiveWarehouse(id: string | null) {
  activeWarehouseId = id;
}

/**
 * The bearer token from sign-in (Keycloak JWT, brokered by the gateway). Set once at login and on
 * reload from storage (see `AuthContext`); every request below carries it so the gateway can authorise.
 */
let authToken: string | null = null;
export function setAuthToken(token: string | null) {
  authToken = token;
}

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
  const headers: Record<string, string> = {};
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (activeWarehouseId) headers[WAREHOUSE_HEADER] = activeWarehouseId;
  if (authToken) headers['Authorization'] = `Bearer ${authToken}`;

  const res = await fetch(`${GATEWAY}/${resource}`, {
    method,
    headers: Object.keys(headers).length ? headers : undefined,
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
