import { useMutation, useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';

/** UC-07 — Stocktake (admin-3-stocktake). */
export type StocktakeState = 'scheduled' | 'counting' | 'review' | 'completed';

export interface StocktakeDiff {
  id: string;
  location: string;
  product: string;
  batch: string;
  system: number;
  counted: number;
  delta: number;
  /** Pre-filled reason, if the operator already assigned one. */
  defaultReason?: StocktakeReason;
}

export const STOCKTAKE_REASONS = ['damage', 'loss', 'pickError', 'countCorrection'] as const;
export type StocktakeReason = (typeof STOCKTAKE_REASONS)[number];

export interface StocktakeSummary {
  id: string;
  title: string;
  sub: string;
  state: StocktakeState;
  locationsCounted: number;
  totalLocations: number;
  matches: number;
  discrepancies: number;
  netVariance: number;
}

export interface Stocktake {
  summary: StocktakeSummary;
  diffs: StocktakeDiff[];
}

/** Row in the stocktake list. */
export interface StocktakeListItem {
  id: string;
  scope: string;
  state: StocktakeState;
  when: string;
  locationsCounted: number;
  totalLocations: number;
  discrepancies: number;
}

export interface ApprovedRow {
  id: string;
  reason: StocktakeReason;
}

export function useStocktakeList() {
  return useQuery({
    queryKey: ['stocktake', 'list'],
    queryFn: () => api.get<StocktakeListItem[]>('inventory/stocktake'),
  });
}

export function useStocktake(id: string | undefined) {
  return useQuery({
    queryKey: ['stocktake', 'detail', id],
    queryFn: () => api.get<Stocktake>(`inventory/stocktake/${id}`),
    enabled: !!id,
  });
}

export function useStartStocktake() {
  return useMutation({
    mutationFn: (body: { scope: string }) => api.post<{ id: string }>('inventory/stocktake', body),
  });
}

export function useApproveStocktake(id: string | undefined) {
  return useMutation({
    mutationFn: (rows: ApprovedRow[]) => api.post(`inventory/stocktake/${id}/approve`, { rows }),
  });
}

/** Re-issue a blind count for the selected (disputed) locations (UC-07). */
export function useRecountStocktake(id: string | undefined) {
  return useMutation({
    mutationFn: (rowIds: string[]) => api.post(`inventory/stocktake/${id}/recount`, { rows: rowIds }),
  });
}
