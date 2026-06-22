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

/** The searchable index. Read-only — Look up never writes to the domain. */
export const getLookupIndex = (): Promise<LookupRow[]> => api.get<LookupRow[]>('lookup/index');

/** Filter the index by free-text query and an optional kind. */
export function searchLookup(rows: LookupRow[], query: string, kind: LookupKind | 'all'): LookupRow[] {
  const q = query.trim().toLowerCase();
  return rows.filter((r) => {
    if (kind !== 'all' && r.kind !== kind) return false;
    if (!q) return true;
    return `${r.title} ${r.meta}`.toLowerCase().includes(q);
  });
}
