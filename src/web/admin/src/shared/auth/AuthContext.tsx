import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import i18n from '@/shared/i18n/i18n';
import { api, setActiveWarehouse } from '@/core/api/client';
import type { CurrentUser } from '@/features/Auth/auth.model';

const STORAGE_KEY = 'wh.currentUser';

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

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(() => {
    const stored = readStored();
    if (stored) void i18n.changeLanguage(stored.language);
    return stored;
  });

  const login = useCallback(async (badge: string) => {
    const u = await api.post<CurrentUser>('auth/login', { badge });
    persist(u);
    setUser(u);
  }, []);

  const logout = useCallback(() => {
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      /* ignore */
    }
    setActiveWarehouse(null);
    setUser(null);
  }, []);

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
