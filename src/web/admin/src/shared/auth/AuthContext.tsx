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
import i18n from '@/shared/i18n/i18n';
import { api, setActiveWarehouse, setAuthToken, setTokenRefresher } from '@/core/api/client';
import { refreshSession, revokeSession } from '@/features/Auth/auth.model';
import type { CurrentUser, LoginResponse } from '@/features/Auth/auth.model';

const STORAGE_KEY = 'wh.currentUser';
const TOKEN_KEY = 'wh.authToken';
const REFRESH_KEY = 'wh.refreshToken';

interface AuthContextValue {
  user: CurrentUser | null;
  /** Resolve a scanned badge to a user; throws (ApiError 401) on unknown badge. */
  login: (badge: string) => Promise<void>;
  logout: () => void;
  /** Replace the cached user after a profile edit (keeps the TopBar in sync). */
  updateUser: (user: CurrentUser) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function readStored(): CurrentUser | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as CurrentUser) : null;
  } catch {
    return null;
  }
}

function persist(user: CurrentUser) {
  void i18n.changeLanguage(user.language);
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
  } catch {
    /* private mode / quota — the session still works, it just won't survive a reload */
  }
}

/** Store (or clear) the tokens: memory (api seam + the refresher's ref) and localStorage (reload-safe). */
function persistTokens(accessToken: string | null, refreshToken: string | null | undefined) {
  setAuthToken(accessToken);
  try {
    if (accessToken) localStorage.setItem(TOKEN_KEY, accessToken);
    else localStorage.removeItem(TOKEN_KEY);
    if (refreshToken) localStorage.setItem(REFRESH_KEY, refreshToken);
    else localStorage.removeItem(REFRESH_KEY);
  } catch {
    /* private mode / quota — the session still works, it just won't survive a reload */
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  // The current refresh token, held in a ref so the (stable) refresher always reads the latest — Keycloak
  // rotates it on every renew.
  const refreshTokenRef = useRef<string | null>(null);

  const [user, setUser] = useState<CurrentUser | null>(() => readStored());

  // Re-arm the api seam with the persisted tokens on mount, so a refresh-safe session stays
  // authorised. Runs once — a ref can't be written during render, so this can't live in the
  // useState initializer above.
  useEffect(() => {
    const stored = readStored();
    if (!stored) return;
    void i18n.changeLanguage(stored.language);
    try {
      const token = localStorage.getItem(TOKEN_KEY);
      if (token) setAuthToken(token);
      refreshTokenRef.current = localStorage.getItem(REFRESH_KEY);
    } catch {
      /* ignore */
    }
  }, []);

  const logout = useCallback(() => {
    const refreshToken = refreshTokenRef.current;
    if (refreshToken) void revokeSession(refreshToken).catch(() => {}); // best-effort session end
    refreshTokenRef.current = null;
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* ignore */
    }
    persistTokens(null, null);
    setActiveWarehouse(null);
    setUser(null);
  }, []);

  const login = useCallback(async (badge: string) => {
    const {
      accessToken,
      refreshToken,
      user: u,
    } = await api.post<LoginResponse>('auth/login', { badge });
    refreshTokenRef.current = refreshToken ?? null;
    persistTokens(accessToken, refreshToken);
    persist(u);
    setUser(u);
  }, []);

  // Silent renew: the api seam calls this when a request 401s on an expired access token. Rotate both
  // tokens; if the refresh token is gone/expired, end the session (the next call then surfaces the 401).
  useEffect(() => {
    setTokenRefresher(async () => {
      const refreshToken = refreshTokenRef.current;
      if (!refreshToken) return null;
      try {
        const renewed = await refreshSession(refreshToken);
        refreshTokenRef.current = renewed.refreshToken ?? null;
        persistTokens(renewed.accessToken, renewed.refreshToken);
        return renewed.accessToken;
      } catch {
        logout();
        return null;
      }
    });
    return () => setTokenRefresher(null);
  }, [logout]);

  const updateUser = useCallback((u: CurrentUser) => {
    persist(u);
    setUser(u);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ user, login, logout, updateUser }),
    [user, login, logout, updateUser],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider');
  return ctx;
}
