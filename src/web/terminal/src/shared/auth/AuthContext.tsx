import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';

import { setActiveWarehouse, setAuthToken } from '@/core/api/client';
import { useI18n } from '@/shared/i18n/i18n';
import {
  login as loginRequest,
  type CurrentOperator,
  type CurrentUser,
} from '@/features/Auth/auth.model';

/**
 * Holds the signed-in operator for the whole app. The terminal has no desk
 * session — the operator badges in on the handheld — so this gate is what
 * `_layout` checks to decide between the login screen and the task stack.
 *
 * The session is persisted per device (like the locale/theme) so a reload in a
 * dead spot doesn't kick the operator back to the badge screen mid-shift. The
 * bearer token + active warehouse are re-armed on the api seam from storage so a
 * reload stays authorised and scoped.
 */
const STORAGE_KEY = 'wms-operator';
const TOKEN_KEY = 'wms-token';

/** Friendly home-site label for the hub identity bar, by warehouse code. */
const SITE_BY_WAREHOUSE: Record<string, string> = {
  WH01: 'Cold-store · Wrocław WH-01',
  WH02: 'Cold-store · Poznań WH-02',
};

/** Map the gateway's desk-user view onto the operator the hub shows. */
function toOperator(user: CurrentUser): CurrentOperator {
  return {
    id: user.id,
    badge: user.badge,
    name: user.name,
    site: SITE_BY_WAREHOUSE[user.defaultWarehouseId] ?? `Warehouse ${user.defaultWarehouseId}`,
    language: user.language,
  };
}

/** Re-arm the api seam (token + warehouse) from a restored session. */
function rearm(user: CurrentUser, token: string | null): void {
  if (token) setAuthToken(token);
  setActiveWarehouse(user.defaultWarehouseId || 'WH01');
}

interface AuthContextValue {
  operator: CurrentOperator | null;
  /** Resolve a scanned badge to an operator; throws (ApiError 401) on unknown badge. */
  signIn: (badge: string) => Promise<void>;
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/** The persisted session — the full user (so the warehouse survives a reload) + the token. */
function readStored(): { user: CurrentUser; token: string | null } | null {
  try {
    const raw = typeof localStorage !== 'undefined' ? localStorage.getItem(STORAGE_KEY) : null;
    if (!raw) return null;
    const user = JSON.parse(raw) as CurrentUser;
    const token = typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null;
    return { user, token };
  } catch {
    return null;
  }
}

function persist(user: CurrentUser | null, token?: string | null): void {
  try {
    if (typeof localStorage === 'undefined') return;
    if (user) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
      if (token) localStorage.setItem(TOKEN_KEY, token);
    } else {
      localStorage.removeItem(STORAGE_KEY);
      localStorage.removeItem(TOKEN_KEY);
    }
  } catch {
    /* private mode / storage unavailable — the session still works in memory */
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const { setLocale } = useI18n();
  const [operator, setOperator] = useState<CurrentOperator | null>(() => {
    const stored = readStored();
    if (!stored) return null;
    // Re-arm the api seam (token + warehouse) so a reload stays authorised and scoped.
    rearm(stored.user, stored.token);
    return toOperator(stored.user);
  });

  const signIn = useCallback(
    async (badge: string) => {
      const { accessToken, user } = await loginRequest(badge);
      setAuthToken(accessToken);
      setActiveWarehouse(user.defaultWarehouseId || 'WH01');
      persist(user, accessToken);
      setLocale(user.language); // honour the operator's preferred language
      setOperator(toOperator(user));
    },
    [setLocale],
  );

  const signOut = useCallback(() => {
    persist(null);
    setAuthToken(null);
    setActiveWarehouse(null);
    setOperator(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ operator, signIn, signOut }),
    [operator, signIn, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
