import { MutationCache, QueryClient } from '@tanstack/react-query';

import { formatApiError } from '@/shared/toast/formatApiError';
import { emitToast } from '@/shared/toast/toastBus';

/**
 * One QueryClient for the app. Admin data is read-heavy and tolerant of brief
 * staleness (it reads cross-service replicas — ADR-0003), so a short stale time
 * avoids refetch storms while keeping data "best known".
 *
 * Every failed write raises a single toast keyed by the domain error code
 * (`errors.<code>`, see `ToastProvider`). Per-mutation `onError` still runs
 * (e.g. QC's optimistic rollback) — this only adds the user-facing notification.
 */
export const queryClient = new QueryClient({
  mutationCache: new MutationCache({
    onError: (error) => emitToast({ variant: 'error', message: formatApiError(error) }),
  }),
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});
