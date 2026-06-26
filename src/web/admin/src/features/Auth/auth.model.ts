/** The three desk roles (terminal operators sign in on the handheld, not here). */
export type UserRole = 'manager' | 'coordinator' | 'inspector';

/** The signed-in desk user. Resolved from a badge scan at sign-in. */
export interface CurrentUser {
  id: string;
  /** Badge number scanned at the login screen. */
  badge: string;
  name: string;
  role: UserRole;
  email: string;
  /** Warehouse the desk opens on after sign-in. */
  defaultWarehouseId: string;
  language: 'en' | 'pl';
}

/** What `POST auth/login` returns: the bearer token (Keycloak JWT, brokered by the gateway) + the user
 * shaped from its claims. In dev the MSW handler returns the same shape with a fake token. */
export interface LoginResponse {
  accessToken: string;
  refreshToken?: string | null;
  expiresIn?: number;
  user: CurrentUser;
}
