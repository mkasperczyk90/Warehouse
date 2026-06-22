import { useMutation, useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';
import type { StatusVariant } from '@/shared/ui';

/*
 * UC-01/UC-02 — Inbound deliveries (admin-2-asn).
 *
 * Talks to the Logistics service directly via the Gateway: `logistics/deliveries/...`. The backend
 * speaks bounded-context vocabulary (delivery + lines, status enum, product/warehouse CODES and a
 * supplier role id) — names live in Catalog/Partners, which this screen does not compose, so where a
 * friendly name is unavailable the code is shown. The model layer below adapts the backend DTOs to
 * the screen's view models.
 */

// ---- Backend DTOs (wire shapes returned by Warehouse.Logistics.Api) --------
interface DeliverySummaryDto {
  id: string;
  warehouseCode: string;
  plannedAt: string;
  status: string;
  lineCount: number;
}

interface DeliveryLineDto {
  lineNo: number;
  productCode: string;
  expectedQuantity: number;
  expectedUnit: string;
  actualQuantity: number | null;
  actualUnit: string | null;
  batchNumber: string | null;
  expiryDate: string | null;
  discrepancy: string;
  note: string | null;
}

interface DeliveryDto {
  id: string;
  supplierRoleId: string;
  warehouseCode: string;
  plannedAt: string;
  status: string;
  slot: { dockCode: string; from: string; to: string } | null;
  lines: DeliveryLineDto[];
}

// ---- Status mapping (DeliveryStatus enum -> badge) -------------------------
const STATUS: Record<string, { variant: StatusVariant; label: string }> = {
  Announced: { variant: 'reserved', label: 'Announced' },
  Arrived: { variant: 'transit', label: 'Arrived' },
  Receiving: { variant: 'transit', label: 'Receiving' },
  Received: { variant: 'transit', label: 'Received' },
  PutAwayInProgress: { variant: 'transit', label: 'Put-away' },
  Completed: { variant: 'available', label: 'Completed' },
  Cancelled: { variant: 'blocked', label: 'Cancelled' },
};

const mapStatus = (s: string) => STATUS[s] ?? { variant: 'reserved' as StatusVariant, label: s };

const fmtDateTime = (iso: string) => {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString();
};

const pad = (n: number) => String(n).padStart(2, '0');
const hhmm = (iso: string) => {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : `${pad(d.getHours())}:${pad(d.getMinutes())}`;
};

const slotLabel = (slot: DeliveryDto['slot']) =>
  slot ? `${slot.dockCode} · ${hhmm(slot.from)}–${hhmm(slot.to)}` : 'slot pending';

// ---- View models (consumed by the screens) --------------------------------
export interface AsnSummary {
  id: string;
  supplier: string;
  status: StatusVariant;
  statusLabel: string;
  meta: string;
}

export interface AsnLine {
  id: string;
  sku: string;
  product: string;
  planned: number;
  unit: string;
  tracking: string;
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

const toSummary = (d: DeliverySummaryDto): AsnSummary => {
  const s = mapStatus(d.status);
  return {
    id: d.id,
    supplier: d.warehouseCode,
    status: s.variant,
    statusLabel: s.label,
    meta: `${fmtDateTime(d.plannedAt)} · ${d.lineCount} lines`,
  };
};

const toLine = (l: DeliveryLineDto): AsnLine => ({
  id: String(l.lineNo),
  sku: l.productCode,
  product: l.productCode,
  planned: l.expectedQuantity,
  unit: l.expectedUnit,
  tracking: l.batchNumber ? `Batch ${l.batchNumber}` : 'Batch + BBE',
});

const toDetail = (d: DeliveryDto): AsnDetail => {
  const s = mapStatus(d.status);
  return {
    id: d.id,
    supplier: d.supplierRoleId,
    warehouse: d.warehouseCode,
    dockSlot: slotLabel(d.slot),
    createdBy: 'Validated against catalog',
    status: s.variant,
    statusLabel: s.label,
    lines: d.lines.map(toLine),
  };
};

export function useAsnList() {
  return useQuery({
    queryKey: ['asn', 'list'],
    queryFn: async () => (await api.get<DeliverySummaryDto[]>('logistics/deliveries')).map(toSummary),
  });
}

export function useAsnDetail(id: string | null | undefined) {
  return useQuery({
    queryKey: ['asn', 'detail', id],
    queryFn: async () => toDetail(await api.get<DeliveryDto>(`logistics/deliveries/${id}`)),
    enabled: !!id,
  });
}

// ---- Create (UC-01: announce) ---------------------------------------------
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
    // `supplierRoleId` carries the supplier identity (a Party-role id in production; a free-text
    // identifier in dev). The dock slot is booked separately after creation (UC-01 step 3).
    mutationFn: (body: NewAsn) =>
      api.post<{ id: string }>('logistics/deliveries', {
        supplierRoleId: body.supplier,
        warehouseCode: body.warehouse,
        plannedAt: new Date().toISOString(),
        lines: body.lines.map((l) => ({ productCode: l.sku, quantity: l.planned, unit: l.unit })),
      }),
  });
}

export const DOCKS = ['D-1', 'D-2', 'D-3', 'D-4', 'D-5', 'D-6'];

/** Parse a "HH:MM–HH:MM" window into today's ISO timestamps; fall back to now .. now+1h. */
function windowToRange(window: string): { from: string; to: string } {
  const m = window.match(/(\d{1,2}):(\d{2})\D+(\d{1,2}):(\d{2})/);
  const base = new Date();
  if (m) {
    const from = new Date(base);
    from.setHours(Number(m[1]), Number(m[2]), 0, 0);
    const to = new Date(base);
    to.setHours(Number(m[3]), Number(m[4]), 0, 0);
    if (to > from) return { from: from.toISOString(), to: to.toISOString() };
  }
  return { from: base.toISOString(), to: new Date(base.getTime() + 3_600_000).toISOString() };
}

/** Assign (or re-assign) a dock slot to a delivery — UC-01 step 3. */
export function useAssignDock(id: string | null | undefined) {
  return useMutation({
    mutationFn: (body: { dock: string; window: string }) =>
      api.post(`logistics/deliveries/${id}/dock-slot`, { dockCode: body.dock, ...windowToRange(body.window) }),
  });
}

/** Mark an announced delivery as arrived at the dock — Announced → Arrived (UC-02). */
export function useMarkArrived(id: string | null | undefined) {
  return useMutation({
    mutationFn: () => api.post(`logistics/deliveries/${id}/arrival`),
  });
}

/**
 * Resolve an unknown-SKU line (UC-01 exception). The backend validates SKUs at announce time and
 * rejects an unknown one outright, so no line is ever flagged here and this path is currently inert;
 * kept so the screen's resolve affordance stays wired until a per-line clarification flow exists.
 */
export function useResolveSku(deliveryId: string | null | undefined) {
  return useMutation({
    mutationFn: (body: { lineId: string; sku: string; product: string; create: boolean }) =>
      api.post(`logistics/deliveries/${deliveryId}/lines/${body.lineId}/resolve`, body),
  });
}

// ---- Receiving progress (derived from the delivery detail) -----------------
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

const RECEIVING_LABEL: Record<ReceivingLineStatus, string> = {
  received: 'Received',
  receiving: 'Receiving',
  pending: 'Pending',
};

function toReceivingLine(l: DeliveryLineDto): ReceivingLine {
  const received = l.actualQuantity ?? 0;
  const status: ReceivingLineStatus =
    received >= l.expectedQuantity && received > 0 ? 'received' : received > 0 ? 'receiving' : 'pending';
  return {
    id: String(l.lineNo),
    sku: l.productCode,
    product: l.productCode,
    expected: l.expectedQuantity,
    received,
    unit: l.expectedUnit,
    status,
    statusLabel: RECEIVING_LABEL[status],
  };
}

export function useReceiving(deliveryId: string | null | undefined) {
  return useQuery({
    queryKey: ['asn', 'receiving', deliveryId],
    queryFn: async (): Promise<ReceivingProgress> => {
      const d = await api.get<DeliveryDto>(`logistics/deliveries/${deliveryId}`);
      const lines = d.lines.map(toReceivingLine);
      return {
        id: d.id,
        supplier: d.supplierRoleId,
        dockSlot: slotLabel(d.slot),
        totalLines: lines.length,
        receivedLines: lines.filter((l) => l.status === 'received').length,
        lines,
      };
    },
    enabled: !!deliveryId,
  });
}
