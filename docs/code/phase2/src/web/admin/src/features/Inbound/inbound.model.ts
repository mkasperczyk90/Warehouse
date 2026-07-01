import { useMutation, useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';
import type { StatusVariant } from '@/shared/ui';

/** UC-01 — Inbound / Advance Shipping Notice (admin-2-asn). */
export interface AsnSummary {
  id: string;
  supplier: string;
  status: StatusVariant;
  statusLabel: string;
  /** One-line schedule summary, e.g. "Today 09:30 · Dock D-3 · 8 lines". */
  meta: string;
}

export interface AsnLine {
  id: string;
  sku: string;
  product: string;
  planned: number;
  unit: string;
  tracking: string;
  /** Unknown SKU / not in catalog — flagged for clarification (UC-01 exception). */
  flagged?: boolean;
}

export interface AsnDetail {
  id: string;
  supplier: string;
  warehouse: string;
  dockSlot: string;
  createdBy: string;
  status: StatusVariant;
  statusLabel: string;
  lines: AsnLine[];
}

export function useAsnList() {
  return useQuery({
    queryKey: ['asn', 'list'],
    queryFn: () => api.get<AsnSummary[]>('asn'),
  });
}

export function useAsnDetail(id: string | null | undefined) {
  return useQuery({
    queryKey: ['asn', 'detail', id],
    queryFn: () => api.get<AsnDetail>(`asn/${id}`),
    enabled: !!id,
  });
}

/** A line drafted while creating an ASN. */
export interface NewAsnLine {
  sku: string;
  product: string;
  planned: number;
  unit: string;
}

export interface NewAsn {
  supplier: string;
  warehouse: string;
  dockSlot: string;
  lines: NewAsnLine[];
}

export function useCreateAsn() {
  return useMutation({
    mutationFn: (body: NewAsn) => api.post<{ id: string }>('asn', body),
  });
}

export const DOCKS = ['D-1', 'D-2', 'D-3', 'D-4', 'D-5', 'D-6'];

/** Assign (or re-assign) a dock slot to an ASN — UC-01 step 3. */
export function useAssignDock(id: string | null | undefined) {
  return useMutation({
    mutationFn: (body: { dock: string; window: string }) => api.post(`asn/${id}/dock`, body),
  });
}

/** Mark an announced delivery as arrived at the dock — Announced → Arrived (UC-02). */
export function useMarkArrived(id: string | null | undefined) {
  return useMutation({
    mutationFn: () => api.post(`asn/${id}/arrive`),
  });
}

/**
 * Read-only receiving progress for an arrived ASN — the coordinator monitors the
 * operator's goods receipt (UC-02) without doing it.
 */
export type ReceivingLineStatus = 'received' | 'receiving' | 'pending';

export interface ReceivingLine {
  id: string;
  sku: string;
  product: string;
  expected: number;
  received: number;
  unit: string;
  status: ReceivingLineStatus;
  statusLabel: string;
}

export interface ReceivingProgress {
  id: string;
  supplier: string;
  dockSlot: string;
  totalLines: number;
  receivedLines: number;
  lines: ReceivingLine[];
}

export function useReceiving(asnId: string | null | undefined) {
  return useQuery({
    queryKey: ['asn', 'receiving', asnId],
    queryFn: () => api.get<ReceivingProgress>(`asn/${asnId}/receiving`),
    enabled: !!asnId,
  });
}

/**
 * Resolve an unknown-SKU line — map it to an existing SKU, or (create=true) create
 * the product in the catalogue and map to it (UC-01 exception → UC-13).
 */
export function useResolveSku(asnId: string | null | undefined) {
  return useMutation({
    mutationFn: (body: { lineId: string; sku: string; product: string; create: boolean }) =>
      api.post(`asn/${asnId}/lines/${body.lineId}/resolve`, body),
  });
}
