import { useCallback, useEffect, useState } from 'react';

/**
 * The state of one async read through the API seam. The terminal has no
 * TanStack Query (the admin does); a single screen rarely fans out to more than
 * one resource, so this minimal hook is enough: fetch once on mount, expose
 * loading / error, and a `reload` for retry. Pair it with `ResourceView`.
 */
export interface Resource<T> {
  data: T | undefined;
  loading: boolean;
  error: Error | undefined;
  reload: () => void;
}

/**
 * Run a parameter-less fetcher once on mount. Pass a stable function reference
 * (the module-level `getX` getters are stable), so the effect runs once.
 */
export function useResource<T>(fetcher: () => Promise<T>): Resource<T> {
  const [state, setState] = useState<{ data?: T; loading: boolean; error?: Error }>({
    loading: true,
  });
  const [nonce, setNonce] = useState(0);

  useEffect(() => {
    let active = true;
    // Keep any data already shown while refetching (e.g. after a mutation's
    // reload), so the screen updates in place instead of flashing a spinner.
    setState((s) => ({ ...s, loading: true, error: undefined }));
    fetcher().then(
      (data) => active && setState({ data, loading: false }),
      (error: unknown) =>
        active &&
        setState({
          loading: false,
          error: error instanceof Error ? error : new Error(String(error)),
        }),
    );
    return () => {
      active = false;
    };
    // `nonce` re-runs the fetch on `reload`; `fetcher` is a stable getter.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [fetcher, nonce]);

  const reload = useCallback(() => setNonce((n) => n + 1), []);

  return { data: state.data, loading: state.loading, error: state.error, reload };
}
