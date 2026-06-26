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
import { AlertCircle, CheckCircle2, Info, X } from 'lucide-react';

import { formatApiError } from './formatApiError';
import { setToastHandler, type ToastInput, type ToastVariant } from './toastBus';
import styles from './Toast.module.css';

interface Toast {
  id: number;
  variant: ToastVariant;
  message: string;
}

export interface ToastApi {
  /** Raise a toast with an explicit variant (defaults to `info`). */
  toast: (input: ToastInput) => void;
  success: (message: string) => void;
  error: (message: string) => void;
  /** Raise an error toast from a thrown request error, keyed by its domain code. */
  apiError: (error: unknown) => void;
  dismiss: (id: number) => void;
}

const ToastContext = createContext<ToastApi | null>(null);

const AUTO_DISMISS_MS = 5000;
const ICON = { success: CheckCircle2, error: AlertCircle, info: Info } as const;

/**
 * App-wide toast notifications. Mount once near the root (see `App.tsx`).
 *
 * Two ways in: `useToast()` for components, and the `toastBus` for non-React
 * callers (the QueryClient surfaces every failed mutation here automatically).
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const nextId = useRef(0);
  const timers = useRef(new Map<number, ReturnType<typeof setTimeout>>());

  const dismiss = useCallback((id: number) => {
    setToasts((cur) => cur.filter((toast) => toast.id !== id));
    const timer = timers.current.get(id);
    if (timer) {
      clearTimeout(timer);
      timers.current.delete(id);
    }
  }, []);

  const push = useCallback(
    (input: ToastInput) => {
      const id = nextId.current++;
      setToasts((cur) => [
        ...cur,
        { id, variant: input.variant ?? 'info', message: input.message },
      ]);
      timers.current.set(
        id,
        setTimeout(() => dismiss(id), AUTO_DISMISS_MS),
      );
    },
    [dismiss],
  );

  // Bridge the non-React bus to this mounted provider.
  useEffect(() => {
    setToastHandler(push);
    return () => setToastHandler(null);
  }, [push]);

  // Clear any pending timers on unmount.
  useEffect(() => {
    const pending = timers.current;
    return () => {
      pending.forEach(clearTimeout);
      pending.clear();
    };
  }, []);

  const api = useMemo<ToastApi>(
    () => ({
      toast: push,
      success: (message) => push({ message, variant: 'success' }),
      error: (message) => push({ message, variant: 'error' }),
      apiError: (error) => push({ message: formatApiError(error), variant: 'error' }),
      dismiss,
    }),
    [push, dismiss],
  );

  return (
    <ToastContext.Provider value={api}>
      {children}
      {toasts.length > 0 ? (
        <div className={styles.viewport} role="region" aria-label="Notifications">
          {toasts.map((toast) => {
            const Icon = ICON[toast.variant];
            return (
              <div
                key={toast.id}
                className={`${styles.toast} ${styles[toast.variant]}`}
                role={toast.variant === 'error' ? 'alert' : 'status'}
              >
                <Icon size={18} aria-hidden className={styles.icon} />
                <span className={styles.message}>{toast.message}</span>
                <button
                  type="button"
                  className={styles.close}
                  aria-label="Dismiss"
                  onClick={() => dismiss(toast.id)}
                >
                  <X size={16} aria-hidden />
                </button>
              </div>
            );
          })}
        </div>
      ) : null}
    </ToastContext.Provider>
  );
}

export function useToast(): ToastApi {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within a ToastProvider');
  return ctx;
}
