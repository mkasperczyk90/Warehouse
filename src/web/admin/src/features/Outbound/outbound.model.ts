import { useMutation, useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';
import type { StatusVariant } from '@/shared/ui';

/** UC-09 — Outbound orders (admin-5-outbound). */
export interface SoSummary {
  id: string;
  customer: string;
  status: StatusVariant;
  statusLabel: string;
  /** e.g. "Required 2026-06-16 · 3 lines". */
  meta: string;
}

export interface SoLine {
  id: string;
  sku: string;
  product: string;
  ordered: number;
  atpAtOrder: number;
  reserved: number;
  status: StatusVariant;
  statusLabel: string;
}

export interface SoDetail {
  id: string;
  customer: string;
  status: StatusVariant;
  statusLabel: string;
  subtitle: string;
  /** Summary cards. */
  linesReserved: string; // e.g. "3 / 3"
  reservedUnits: number;
  shipTo: string;
  lines: SoLine[];
}

export function useOrderList() {
  return useQuery({
    queryKey: ['so', 'list'],
    queryFn: () => api.get<SoSummary[]>('orders'),
  });
}

export function useOrderDetail(id: string | null | undefined) {
  return useQuery({
    queryKey: ['so', 'detail', id],
    queryFn: () => api.get<SoDetail>(`orders/${id}`),
    enabled: !!id,
  });
}

/** A line drafted while creating an order. */
export interface NewOrderLine {
  sku: string;
  product: string;
  ordered: number;
}

export interface NewOrder {
  customer: string;
  shipTo: string;
  requiredDate: string;
  lines: NewOrderLine[];
}

export function useCreateOrder() {
  return useMutation({
    mutationFn: (body: NewOrder) => api.post<{ id: string }>('orders', body),
  });
}

export type OrderDecision = 'split' | 'hold';

/** Resolve a partial/waiting order — split (ship now, backorder rest) or hold (UC-09). */
export function useDecideOrder(id: string | null | undefined) {
  return useMutation({
    mutationFn: (body: { decision: OrderDecision }) => api.post(`orders/${id}/decision`, body),
  });
}

/** Release a reserved order to a picking wave — Reserved → Picking (UC-09 → UC-10). */
export function useReleaseOrder(id: string | null | undefined) {
  return useMutation({
    mutationFn: () => api.post(`orders/${id}/release`),
  });
}

/** Cancel an order — reservations released back to ATP (UC-09 lifecycle). */
export function useCancelOrder(id: string | null | undefined) {
  return useMutation({
    mutationFn: () => api.post(`orders/${id}/cancel`),
  });
}

/** A line's ATP broken down by location — the "why is this partial" drill. */
export interface AtpRow {
  location: string;
  room: string;
  onHand: number;
  atp: number;
  status: StatusVariant;
  statusLabel: string;
}

export function useSkuStock(sku: string | null | undefined) {
  return useQuery({
    queryKey: ['stock', 'by-sku', sku],
    queryFn: () => api.get<AtpRow[]>(`stock/by-sku/${sku}`),
    enabled: !!sku,
  });
}
