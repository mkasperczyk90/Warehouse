import { useMutation, useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';
import type { StatusVariant } from '@/shared/ui';

/** UC-05 — Stock view (admin-1-stock). One on-hand row, with domain status. */
export interface StockRow {
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
  status: StatusVariant;
  /** Custom badge label (e.g. "Expiring 1d", "Blocked · QC"). */
  statusLabel: string;
}

export interface StockKpis {
  onHand: number;
  atp: number;
  reserved: number;
  blockedExpiring: number;
  unit: string;
}

export function useStockRows() {
  return useQuery({
    queryKey: ['stock', 'rows'],
    queryFn: () => api.get<StockRow[]>('inventory/stock/rows'),
  });
}

export function useStockKpis() {
  return useQuery({
    queryKey: ['stock', 'kpis'],
    queryFn: () => api.get<StockKpis>('inventory/stock/kpis'),
  });
}

/** A ledger movement on a stock item — the immutable history behind a balance. */
export interface StockMovementRow {
  id: string;
  date: string;
  type: string;
  qty: number;
  reference: string;
}

/** Drill-down for one stock item: the balance breakdown + its movement history. */
export interface StockItemDetail {
  id: string;
  product: string;
  sku: string;
  batch: string;
  bestBefore: string;
  location: string;
  room: string;
  onHand: number;
  atp: number;
  reserved: number;
  unit: string;
  status: StatusVariant;
  statusLabel: string;
  movements: StockMovementRow[];
}

export function useStockItem(id: string | undefined) {
  return useQuery({
    queryKey: ['stock', 'item', id],
    queryFn: () => api.get<StockItemDetail>(`inventory/stock/item/${id}`),
    enabled: !!id,
  });
}

// --- Row actions: move & block -------------------------------------------
export const BLOCK_REASONS = ['damage', 'contamination', 'recall', 'inspection'] as const;
export type BlockReason = (typeof BLOCK_REASONS)[number];

/** A candidate target location for a move, with its room's temperature class. */
export interface MoveLocation {
  address: string;
  room: string;
  /** 'cold' | 'freezer' | 'standard' | 'hazmat' | 'dock'. */
  roomType: string;
}

export function useLocations() {
  return useQuery({
    queryKey: ['locations'],
    queryFn: () => api.get<MoveLocation[]>('inventory/locations'),
  });
}

export function useMoveStock(id: string | undefined) {
  return useMutation({
    mutationFn: (body: { toLocation: string }) => api.post(`inventory/stock/item/${id}/move`, body),
  });
}

export function useBlockStock(id: string | undefined) {
  return useMutation({
    mutationFn: (body: { reason: BlockReason; note?: string }) =>
      api.post(`inventory/stock/item/${id}/block`, body),
  });
}
