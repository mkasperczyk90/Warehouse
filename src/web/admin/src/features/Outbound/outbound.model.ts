import { useMutation, useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';
import type { StatusVariant } from '@/shared/ui';

/*
 * UC-09…UC-12 — Outbound orders (admin-5-outbound), talking to the Logistics service directly:
 * `logistics/orders/...`. The backend models the order lifecycle (reservations live in Inventory and
 * are not composed here), so the reservation view is derived from the order STATUS and product names
 * fall back to codes. Split/hold (decision) and the per-location ATP drill have no backend yet and
 * stay on their mock routes.
 */

// ---- Backend DTOs ----------------------------------------------------------
interface OrderSummaryDto {
  id: string;
  warehouseCode: string;
  requiredAt: string;
  status: string;
  lineCount: number;
}

interface OrderLineDto {
  lineNo: number;
  productCode: string;
  quantity: number;
  unit: string;
}

interface OrderDto {
  id: string;
  customerRoleId: string;
  warehouseCode: string;
  requiredAt: string;
  status: string;
  shipTo: { street: string; city: string; postalCode: string; countryCode: string };
  lines: OrderLineDto[];
}

// ---- Status mapping (OrderStatus enum) -------------------------------------
const STATUS: Record<string, { variant: StatusVariant; label: string; subtitle: string }> = {
  Created: { variant: 'reserved', label: 'Created', subtitle: 'Created · not yet reserved against ATP' },
  PartiallyReserved: {
    variant: 'transit',
    label: 'Partially reserved',
    subtitle: 'One line short of ATP — partial / waiting decision pending',
  },
  Reserved: {
    variant: 'reserved',
    label: 'Reserved',
    subtitle: 'Soft reservation against ATP · available portion reserved, batch+location pinned by FEFO at wave release',
  },
  Picking: { variant: 'available', label: 'Picking', subtitle: 'Released to wave — FEFO allocation, picking in progress' },
  Packed: { variant: 'available', label: 'Packed', subtitle: 'Packed — ready for carrier collection' },
  Dispatched: { variant: 'available', label: 'Dispatched', subtitle: 'Dispatched — collected by carrier' },
  Cancelled: { variant: 'blocked', label: 'Cancelled', subtitle: 'Cancelled — reservations released back to ATP' },
};

const mapStatus = (s: string) => STATUS[s] ?? STATUS.Created;
const RESERVED_STATES = new Set(['Reserved', 'Picking', 'Packed', 'Dispatched']);
const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toISOString().slice(0, 10);
};

// ---- View models -----------------------------------------------------------
export interface SoSummary {
  id: string;
  customer: string;
  status: StatusVariant;
  statusLabel: string;
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
  linesReserved: string;
  reservedUnits: number;
  shipTo: string;
  lines: SoLine[];
}

const toSummary = (o: OrderSummaryDto): SoSummary => {
  const s = mapStatus(o.status);
  return {
    id: o.id,
    customer: o.warehouseCode,
    status: s.variant,
    statusLabel: s.label,
    meta: `Required ${fmtDate(o.requiredAt)} · ${o.lineCount} lines`,
  };
};

/** Derive the per-line reservation view from the order's lifecycle status. */
function reservedLines(status: string, lines: OrderLineDto[]): SoLine[] {
  return lines.map((l, i) => {
    const base = { id: String(l.lineNo), sku: l.productCode, product: l.productCode, ordered: l.quantity, atpAtOrder: 0 };
    if (status === 'PartiallyReserved' && i === 0) {
      return { ...base, reserved: Math.floor(l.quantity / 2), status: 'transit' as StatusVariant, statusLabel: 'Partial' };
    }
    if (status === 'PartiallyReserved' || RESERVED_STATES.has(status)) {
      return { ...base, reserved: l.quantity, status: 'reserved' as StatusVariant, statusLabel: 'Reserved' };
    }
    const label = status === 'Cancelled' ? 'Cancelled' : 'Created';
    return { ...base, reserved: 0, status: 'reserved' as StatusVariant, statusLabel: label };
  });
}

const toDetail = (o: OrderDto): SoDetail => {
  const s = mapStatus(o.status);
  const lines = reservedLines(o.status, o.lines);
  const reservedCount = lines.filter((l) => l.reserved >= l.ordered && l.ordered > 0).length;
  return {
    id: o.id,
    customer: o.customerRoleId,
    status: s.variant,
    statusLabel: s.label,
    subtitle: s.subtitle,
    linesReserved: `${reservedCount} / ${lines.length}`,
    reservedUnits: lines.reduce((sum, l) => sum + l.reserved, 0),
    shipTo: `${o.shipTo.city}, ${o.shipTo.street}`,
    lines,
  };
};

export function useOrderList() {
  return useQuery({
    queryKey: ['so', 'list'],
    queryFn: async () => (await api.get<OrderSummaryDto[]>('logistics/orders')).map(toSummary),
  });
}

export function useOrderDetail(id: string | null | undefined) {
  return useQuery({
    queryKey: ['so', 'detail', id],
    queryFn: async () => toDetail(await api.get<OrderDto>(`logistics/orders/${id}`)),
    enabled: !!id,
  });
}

// ---- Create (UC-09) --------------------------------------------------------
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
    mutationFn: (body: NewOrder) =>
      api.post<{ id: string }>('logistics/orders', {
        customerRoleId: body.customer,
        shipTo: { street: body.shipTo || '—', city: '—', postalCode: '00-000', countryCode: 'PL' },
        warehouseCode: 'WH01',
        requiredAt: body.requiredDate ? new Date(body.requiredDate).toISOString() : new Date().toISOString(),
        lines: body.lines.map((l) => ({ productCode: l.sku, quantity: l.ordered, unit: 'ea' })),
      }),
  });
}

export type OrderDecision = 'split' | 'hold';

/** Resolve a partial/waiting order — split or hold (UC-09). No backend yet; handled by the mock. */
export function useDecideOrder(id: string | null | undefined) {
  return useMutation({
    mutationFn: (body: { decision: OrderDecision }) => api.post(`logistics/orders/${id}/decision`, body),
  });
}

/** Release a reserved order to a picking wave — Reserved → Picking (UC-10). */
export function useReleaseOrder(id: string | null | undefined) {
  return useMutation({
    mutationFn: () => api.post(`logistics/orders/${id}/picking`),
  });
}

/** Cancel an order — reservations released back to ATP (UC-09 lifecycle). */
export function useCancelOrder(id: string | null | undefined) {
  return useMutation({
    mutationFn: () => api.post(`logistics/orders/${id}/cancel`),
  });
}

// ---- ATP drill (separate stock concern; no order-backend) ------------------
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
