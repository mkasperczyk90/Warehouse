/**
 * A tiny pub/sub bridge so non-React callers can raise toasts.
 *
 * The QueryClient's mutation cache (see `core/api/queryClient.ts`) lives outside
 * the React tree, so it can't call `useToast()`. It emits here instead; the
 * mounted `ToastProvider` registers its `push` as the handler. One provider is
 * mounted at a time, so a single handler slot is enough.
 */
export type ToastVariant = 'success' | 'error' | 'info';

export interface ToastInput {
  message: string;
  variant?: ToastVariant;
}

type Handler = (toast: ToastInput) => void;

let handler: Handler | null = null;

export function setToastHandler(next: Handler | null) {
  handler = next;
}

export function emitToast(toast: ToastInput) {
  handler?.(toast);
}
