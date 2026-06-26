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

/**
 * Wire shape of the Topology backend's site summary (`GET /topology/warehouses` → `WarehouseSummaryDto`,
 * behind the gateway at `/api/topology`). The switcher consumes this directly, so going live is turning
 * MSW off (ADR-0006).
 */
export interface WarehouseSummary {
  code: string;
  name: string;
  city: string;
  countryCode: string;
  roomCount: number;
  dockCount: number;
  locationCount: number;
}

/** "WH-01 Wrocław" — the label used in the TopBar and pickers. */
export function warehouseLabel(w: Warehouse): string {
  return `${w.code} ${w.name}`;
}

export function useWarehouses() {
  return useQuery({
    queryKey: ['warehouses'],
    // Topology owns the sites; the switcher shows code + city, and the warehouse code is the identity
    // sent as `X-Warehouse-Id`.
    queryFn: async () => {
      const sites = await api.get<WarehouseSummary[]>('topology/warehouses');
      return sites.map<Warehouse>((w) => ({
        id: w.code,
        code: w.code,
        name: w.city,
      }));
    },
    // The site list rarely changes within a session.
    staleTime: Infinity,
  });
}
