import { api } from '@/core/api/client';
import type { IconName } from '@/shared/ui';
import type { StatusKey } from '@/shared/theme/tokens';

export type LookupKind = 'product' | 'location' | 'batch';

export interface LookupRow {
  id: string;
  kind: LookupKind;
  icon: IconName;
  title: string;
  /** Secondary detail line (data; not localized). */
  meta: string;
  /** On-hand / ATP quantity, when the row is a stock figure. */
  qty?: { value: number | string; unit: string };
  /** Stock status — its label is resolved from the catalogue by `kind`. */
  status?: { kind: StatusKey };
}

export const KIND_FILTERS: { key: LookupKind | 'all' }[] = [
  { key: 'all' },
  { key: 'product' },
  { key: 'location' },
  { key: 'batch' },
];

/** The Inventory stock-view row (admin contract) — one on-hand line per SKU/batch/location. */
interface StockRowDto {
  id: string;
  product: string;
  sku: string;
  batch: string;
  bestBefore: string;
  location: string;
  room: string;
  onHand: number;
  atp: number;
  unit: string;
  status: string;
}

/** A known storage location (admin move-target picker contract). */
interface LocationDto {
  address: string;
  room: string;
  roomType: string;
}

/**
 * The searchable index, built from real Inventory reads (stock rows + known locations). Read-only —
 * Look up never writes to the domain. The backend deals in product codes (no composed names), so a
 * product row falls back to its SKU.
 */
export const getLookupIndex = async (): Promise<LookupRow[]> => {
  const [rows, locations] = await Promise.all([
    api.get<StockRowDto[]>('inventory/stock/rows'),
    api.get<LocationDto[]>('inventory/locations'),
  ]);

  // One batch row per stock line — titled by batch + product name (the stock view composes the name).
  const batches: LookupRow[] = rows.map((r) => ({
    id: `b-${r.id}`,
    kind: 'batch',
    icon: 'batch',
    title: r.batch === '—' ? r.product : `${r.batch} · ${r.product}`,
    meta: `BBE ${r.bestBefore} · ${r.location}`,
    qty: { value: r.onHand, unit: r.unit },
    status: { kind: r.status as StatusKey },
  }));

  // One product row per SKU, summing available-to-promise across its locations.
  const bySku = new Map<string, { name: string; atp: number; unit: string; locations: number; status: string }>();
  for (const r of rows) {
    const agg = bySku.get(r.sku) ?? { name: r.product, atp: 0, unit: r.unit, locations: 0, status: r.status };
    agg.atp += r.atp;
    agg.locations += 1;
    if (r.status === 'blocked' || r.status === 'expired') agg.status = r.status;
    bySku.set(r.sku, agg);
  }
  const products: LookupRow[] = [...bySku.entries()].map(([sku, agg]) => ({
    id: `p-${sku}`,
    kind: 'product',
    icon: 'product',
    title: agg.name,
    meta: `SKU ${sku} · ATP across ${agg.locations} location${agg.locations === 1 ? '' : 's'}`,
    qty: { value: agg.atp, unit: agg.unit },
    status: { kind: agg.status as StatusKey },
  }));

  const places: LookupRow[] = locations.map((l) => ({
    id: `l-${l.address}`,
    kind: 'location',
    icon: 'location',
    title: l.address,
    meta: `${l.roomType} · room ${l.room}`,
  }));

  return [...products, ...batches, ...places];
};

/** Filter the index by free-text query and an optional kind. */
export function searchLookup(rows: LookupRow[], query: string, kind: LookupKind | 'all'): LookupRow[] {
  const q = query.trim().toLowerCase();
  return rows.filter((r) => {
    if (kind !== 'all' && r.kind !== kind) return false;
    if (!q) return true;
    return `${r.title} ${r.meta}`.toLowerCase().includes(q);
  });
}
