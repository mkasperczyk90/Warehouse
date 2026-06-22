import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { api } from '@/core/api/client';
import type { StatusVariant } from '@/shared/ui';

/** UC-12 — Dispatch board (admin-6-dispatch). A shipment card on the board. */
export interface Shipment {
  id: string;
  customer: string;
  /** Package/weight summary, e.g. "2 pkg · 28 kg". */
  summary: string;
  carrier?: { code: string; name: string };
  pickup?: string;
  badge?: { variant: StatusVariant; label: string };
  /** Tracking line for dispatched shipments. */
  tracking?: string;
  /** Show the "Assign carrier" affordance (packed, no carrier yet). */
  canAssign?: boolean;
}

/** A board column; `key` resolves to its i18n title (`dispatch.col.<key>`). */
export interface DispatchColumn {
  key: string;
  shipments: Shipment[];
}

export function useDispatchBoard() {
  return useQuery({
    queryKey: ['dispatch', 'board'],
    queryFn: () => api.get<DispatchColumn[]>('dispatch/board'),
  });
}

export const CARRIERS = [
  { code: 'DH', name: 'DHL' },
  { code: 'GL', name: 'GLS' },
  { code: 'DP', name: 'DPD' },
] as const;

/** Assign a carrier to a packed shipment — moves it to "Carrier assigned". */
export function useAssignCarrier() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { id: string; carrierCode: string; pickup: string }) =>
      api.post(`dispatch/${body.id}/assign`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['dispatch', 'board'] }),
  });
}

/** Advance a shipment to the next column (assigned → notice sent → dispatched). */
export function useAdvanceShipment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => api.post(`dispatch/${id}/advance`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['dispatch', 'board'] }),
  });
}
