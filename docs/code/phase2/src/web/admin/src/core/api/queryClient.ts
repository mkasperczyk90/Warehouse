import { QueryClient } from '@tanstack/react-query';

/**
 * One QueryClient for the app. Admin data is read-heavy and tolerant of brief
 * staleness (it reads cross-service replicas — ADR-0003), so a short stale time
 * avoids refetch storms while keeping data "best known".
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});
