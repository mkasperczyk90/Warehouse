import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';

import { useI18n } from '@/shared/i18n/i18n';
import { login as loginRequest, type CurrentOperator } from '@/features/Auth/auth.model';

/**
 * Holds the signed-in operator for the whole app. The terminal has no desk
 * session — the operator badges in on the handheld — so this gate is what
 * `_layout` checks to decide between the login screen and the task stack.
 *
 * The session is persisted per device (like the locale/theme) so a reload in a
 * dead spot doesn't kick the operator back to the badge screen mid-shift.
 */
const STORAGE_KEY = 'wms-operator';

interface AuthContextValue {
  operator: CurrentOperator | null;
  /** Resolve a scanned badge to an operator; throws (ApiError 401) on unknown badge. */
  signIn: (badge: string) => Promise<void>;
  signOut: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function readStored(): CurrentOperator | null {
  try {
    const raw = typeof localStorage !== 'undefined' ? localStorage.getItem(STORAGE_KEY) : null;
    return raw ? (JSON.parse(raw) as CurrentOperator) : null;
  } catch {
    return null;
  }
}

function persist(operator: CurrentOperator | null): void {
  try {
    if (typeof localStorage === 'undefined') return;
    if (operator) localStorage.setItem(STORAGE_KEY, JSON.stringify(operator));
    else localStorage.removeItem(STORAGE_KEY);
  } catch {
    /* private mode / storage unavailable — the session still works in memory */
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const { setLocale } = useI18n();
  const [operator, setOperator] = useState<CurrentOperator | null>(readStored);

  const signIn = useCallback(
    async (badge: string) => {
      const op = await loginRequest(badge);
      persist(op);
      setLocale(op.language); // honour the operator's preferred language
      setOperator(op);
    },
    [setLocale],
  );

  const signOut = useCallback(() => {
    persist(null);
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
