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

/** Resolve a scanned badge to an operator; rejects (ApiError 401) on unknown badge. */
export const login = (badge: string): Promise<CurrentOperator> =>
  api.post<CurrentOperator>('auth/login', { badge });
