/**
 * Typed URL search params for the Stock view, so filters are deep-linkable and
 * refresh-safe (a shared/bookmarked `/stock?pill=blocked&q=milk` reopens filtered).
 *
 * Lives in its own leaf module — `router.tsx` imports the validator without
 * pulling the (lazy-loaded) screen into the initial chunk.
 */
export type PillKey = 'all' | 'coldRoom' | 'blocked' | 'expiring';

const PILL_KEYS: PillKey[] = ['all', 'coldRoom', 'blocked', 'expiring'];

export interface StockSearch {
  /** Free-text filter (SKU / name / batch / location). */
  q?: string;
  /** Active status pill; `all` is the default and stays out of the URL. */
  pill?: PillKey;
}

export function validateStockSearch(search: Record<string, unknown>): StockSearch {
  const pill = search.pill;
  return {
    q: typeof search.q === 'string' && search.q !== '' ? search.q : undefined,
    pill: PILL_KEYS.includes(pill as PillKey) && pill !== 'all' ? (pill as PillKey) : undefined,
  };
}
