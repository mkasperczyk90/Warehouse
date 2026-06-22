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
