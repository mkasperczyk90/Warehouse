/**
 * Shared URL search schema for master-detail screens: the selected record id is
 * carried in the URL (`?selected=…`) so a selection is deep-linkable and survives
 * a refresh. Kept here (a leaf module, no heavy imports) so `router.tsx` can wire
 * `validateSearch` without pulling a lazy-loaded screen into the initial chunk.
 */
export interface SelectionSearch {
  selected?: string;
}

export function validateSelectionSearch(search: Record<string, unknown>): SelectionSearch {
  return {
    selected:
      typeof search.selected === 'string' && search.selected !== '' ? search.selected : undefined,
  };
}
