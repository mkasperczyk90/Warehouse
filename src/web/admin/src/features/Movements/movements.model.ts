import { useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';

/** A row of the immutable stock-movement ledger (ADR-0002; stock is its projection). */
export const MOVEMENT_TYPES = ['receipt', 'putaway', 'pick', 'move', 'adjustment'] as const;
export type MovementType = (typeof MOVEMENT_TYPES)[number];

export interface MovementRow {
  id: string;
  date: string;
  type: MovementType;
  typeLabel: string;
  product: string;
  sku: string;
  batch: string;
  location: string;
  /** Signed: positive = into stock, negative = out. */
  qty: number;
  unit: string;
  reference: string;
}

export function useMovements() {
  return useQuery({
    queryKey: ['movements'],
    queryFn: () => api.get<MovementRow[]>('movements'),
  });
}
