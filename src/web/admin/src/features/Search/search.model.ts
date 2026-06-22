import { useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';

/** A global-search hit — the desk's "where is X" across the whole admin. */
export type SearchType = 'product' | 'stock' | 'asn' | 'order' | 'shipment' | 'location';

export interface SearchResult {
  type: SearchType;
  /** The id/sku used to navigate to the hit. */
  refId: string;
  label: string;
  sublabel: string;
}

export function useGlobalSearch(query: string) {
  const q = query.trim();
  return useQuery({
    queryKey: ['search', q],
    queryFn: () => api.get<SearchResult[]>(`search?q=${encodeURIComponent(q)}`),
    enabled: q.length >= 2,
  });
}
