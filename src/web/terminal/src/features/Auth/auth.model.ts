import { api } from '@/core/api/client';
import type { Locale } from '@/shared/i18n/i18n';

/**
 * The signed-in floor operator. Resolved from a badge scan at the login screen
 * (operators sign in on the handheld, not the desk — see the admin's auth note).
 */
export interface CurrentOperator {
  id: string;
  /** Badge number scanned at the login screen. */
  badge: string;
  name: string;
  /** Home site shown in the hub identity bar. */
  site: string;
  /** Preferred interface language — applied to the device on sign-in. */
  language: Locale;
}

/**
 * The desk-user view the gateway shapes from the Keycloak token claims (same shape the admin gets).
 * The terminal only needs a subset; it maps this onto {@link CurrentOperator} in the AuthContext.
 */
export interface CurrentUser {
  id: string;
  badge: string;
  name: string;
  role: string;
  email: string;
  /** Warehouse the operator works in — used to scope every request. */
  defaultWarehouseId: string;
  language: Locale;
}

/** What `POST auth/login` returns: the bearer token (Keycloak JWT, brokered by the gateway) + the user
 *  shaped from its claims. The terminal stores the token and carries it on every request. */
export interface LoginResponse {
  accessToken: string;
  refreshToken?: string | null;
  expiresIn?: number;
  user: CurrentUser;
}

/** Resolve a scanned badge to a token + user; rejects (ApiError 401) on unknown badge. */
export const login = (badge: string): Promise<LoginResponse> =>
  api.post<LoginResponse>('auth/login', { badge });
