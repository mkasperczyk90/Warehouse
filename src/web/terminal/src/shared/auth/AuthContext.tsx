import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';

import { setActiveWarehouse, setAuthToken, setTokenRefresher } from '@/core/api/client';
import { useI18n } from '@/shared/i18n/i18n';
import {
  login as loginRequest,
  refreshSession,
  revokeSession,
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
 * reload stays authorised and scoped. When the access token expires mid-shift the
 * api seam silently renews it from the stored refresh token — no badge re-scan.
 */
const STORAGE_KEY = 'wms-operator';
const TOKEN_KEY = 'wms-token';
const REFRESH_KEY = 'wms-refresh';

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

/** The persisted session — the full user (so the warehouse survives a reload) + both tokens. */
function readStored(): {
  user: CurrentUser;
  token: string | null;
  refreshToken: string | null;
} | null {
  try {
    const raw = typeof localStorage !== 'undefined' ? localStorage.getItem(STORAGE_KEY) : null;
    if (!raw) return null;
    const user = JSON.parse(raw) as CurrentUser;
    const token = typeof localStorage !== 'undefined' ? localStorage.getItem(TOKEN_KEY) : null;
    const refreshToken =
      typeof localStorage !== 'undefined' ? localStorage.getItem(REFRESH_KEY) : null;
    return { user, token, refreshToken };
  } catch {
    return null;
  }
}

function persist(
  user: CurrentUser | null,
  token?: string | null,
  refreshToken?: string | null,
): void {
  try {
    if (typeof localStorage === 'undefined') return;
    if (user) {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
      if (token) localStorage.setItem(TOKEN_KEY, token);
      if (refreshToken) localStorage.setItem(REFRESH_KEY, refreshToken);
    } else {
      localStorage.removeItem(STORAGE_KEY);
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(REFRESH_KEY);
    }
  } catch {
    /* private mode / storage unavailable — the session still works in memory */
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const { setLocale } = useI18n();
  // The current refresh token, in a ref so the (stable) refresher always reads the latest — Keycloak
  // rotates it on every renew.
  const refreshTokenRef = useRef<string | null>(null);

  const [operator, setOperator] = useState<CurrentOperator | null>(() => {
    const stored = readStored();
    if (!stored) return null;
    // Re-arm the api seam (token + warehouse) so a reload stays authorised and scoped.
    rearm(stored.user, stored.token);
    refreshTokenRef.current = stored.refreshToken;
    return toOperator(stored.user);
  });

  const signIn = useCallback(
    async (badge: string) => {
      const { accessToken, refreshToken, user } = await loginRequest(badge);
      setAuthToken(accessToken);
      setActiveWarehouse(user.defaultWarehouseId || 'WH01');
      refreshTokenRef.current = refreshToken ?? null;
      persist(user, accessToken, refreshToken);
      setLocale(user.language); // honour the operator's preferred language
      setOperator(toOperator(user));
    },
    [setLocale],
  );

  const signOut = useCallback(() => {
    const refreshToken = refreshTokenRef.current;
    if (refreshToken) void revokeSession(refreshToken).catch(() => {}); // best-effort session end
    refreshTokenRef.current = null;
    persist(null);
    setAuthToken(null);
    setActiveWarehouse(null);
    setOperator(null);
  }, []);

  // Silent renew: the api seam calls this when a request 401s on an expired access token. Rotate both
  // tokens; if the refresh token is gone/expired, sign out (the next call then surfaces the 401).
  useEffect(() => {
    setTokenRefresher(async () => {
      const refreshToken = refreshTokenRef.current;
      if (!refreshToken) return null;
      try {
        const renewed = await refreshSession(refreshToken);
        refreshTokenRef.current = renewed.refreshToken ?? null;
        setAuthToken(renewed.accessToken);
        persist(renewed.user, renewed.accessToken, renewed.refreshToken);
        return renewed.accessToken;
      } catch {
        signOut();
        return null;
      }
    });
    return () => setTokenRefresher(null);
  }, [signOut]);

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
