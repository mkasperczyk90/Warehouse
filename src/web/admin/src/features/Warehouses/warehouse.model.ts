import { useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';

/**
 * A warehouse the desk can switch between. The active one scopes every query
 * (the api client sends it as `X-Warehouse-Id`); see `WarehouseContext`.
 */
export interface Warehouse {
  id: string;
  /** Short code shown in the switcher, e.g. "WH-01". */
  code: string;
  /** City / site name, e.g. "Wrocław". */
  name: string;
}

/** "WH-01 Wrocław" — the label used in the TopBar and pickers. */
export function warehouseLabel(w: Warehouse): string {
  return `${w.code} ${w.name}`;
}

export function useWarehouses() {
  return useQuery({
    queryKey: ['warehouses'],
    queryFn: () => api.get<Warehouse[]>('warehouses'),
    // The site list rarely changes within a session.
    staleTime: Infinity,
  });
}
