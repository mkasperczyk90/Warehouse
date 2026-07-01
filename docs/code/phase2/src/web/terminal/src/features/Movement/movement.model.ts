import { api } from '@/core/api/client';

/** UC-06 — Move / replenish. */
export interface MoveTask {
  task: number;
  ofTasks: number;
  from: string;
  to: string;
  sku: string;
  batch: string;
  product: string;
  coldChip: string;
  bbeChip: string;
  qty: number;
  checks: string[];
}

export const getMoveTask = (): Promise<MoveTask> => api.get<MoveTask>('move/next');

/** Confirm the move within the warehouse → ledger Move movement. */
export const confirmMove = (qty: number): Promise<void> => api.post<void>('move/next/confirm', { qty });

/** Inter-warehouse transfer → goods leave as InTransit (UC-06). */
export const transferMove = (qty: number): Promise<void> => api.post<void>('move/next/transfer', { qty });
