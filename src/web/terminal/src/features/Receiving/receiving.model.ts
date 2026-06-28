import { api } from '@/core/api/client';
import { humanRef } from '@/shared/format/ref';

/*
 * UC-02 — Goods receipt (one delivery line), talking to the Logistics service directly:
 * GET/POST `logistics/deliveries/{id}/...`. The backend keys deliveries by GUID and deals in codes
 * (no supplier/product names, which live in Partners/Catalog and aren't composed here), so those
 * fields fall back to codes and a friendly ref. The terminal receives the first WH01 delivery that
 * still has stock to book in.
 */

const WAREHOUSE = 'WH01';
// A delivery is bookable from announcement through receiving; once Received it drops off the worklist.
const RECEIVABLE = new Set(['Announced', 'Arrived', 'Receiving']);

interface DeliverySummaryDto {
  id: string;
  warehouseCode: string;
  status: string;
  lineCount: number;
}

/** The delivery + line the terminal is currently receiving (set by getReceipt, used by confirm). */
let current: { deliveryId: string; lineNo: number; unit: string } | null = null;

export interface Receipt {
  asn: string;
  supplier: string;
  dock: string;
  line: number;
  ofLines: number;
  sku: string;
  product: string;
  expectedQty: number;
  expectedNote: string;
  unit: string;
  batch: string;
  bestBefore: string;
}

interface DeliveryLineDto {
  lineNo: number;
  productCode: string;
  expectedQuantity: number;
  expectedUnit: string;
  batchNumber: string | null;
  expiryDate: string | null;
}

interface DeliveryDto {
  id: string;
  supplierRoleId: string;
  warehouseCode: string;
  status: string;
  slot: { dockCode: string } | null;
  lines: DeliveryLineDto[];
}

export const getReceipt = async (): Promise<Receipt> => {
  const list = await api.get<DeliverySummaryDto[]>('logistics/deliveries');
  const pick =
    list.find((d) => d.warehouseCode === WAREHOUSE && RECEIVABLE.has(d.status)) ??
    list.find((d) => d.warehouseCode === WAREHOUSE) ??
    list[0];
  const d = await api.get<DeliveryDto>(`logistics/deliveries/${pick.id}`);
  const line = d.lines[0];
  current = { deliveryId: d.id, lineNo: line.lineNo, unit: line.expectedUnit };
  return {
    asn: humanRef('ASN', d.id),
    supplier: d.supplierRoleId,
    dock: d.slot ? `Dock ${d.slot.dockCode}` : 'Dock —',
    line: line.lineNo,
    ofLines: d.lines.length,
    sku: line.productCode,
    product: line.productCode,
    expectedQty: line.expectedQuantity,
    expectedNote: '',
    unit: line.expectedUnit,
    batch: line.batchNumber ?? '',
    bestBefore: line.expiryDate ?? '',
  };
};

/** Why a counted quantity differs from the ASN (UC-02 · exceptions §Inbound). */
export type DiscrepancyReason = 'shortage' | 'overage' | 'damage';

const DISCREPANCY: Record<DiscrepancyReason, string> = {
  shortage: 'Shortage',
  overage: 'Overage',
  damage: 'Damaged',
};

export interface ConfirmReceiptBody {
  counted: number;
  /** A plain confirm, or a recorded discrepancy with its reason. */
  reason?: DiscrepancyReason;
}

/** Confirm one delivery line — receipt proceeds; a discrepancy is recorded, not blocked.
 *  Drives the delivery through its receipt lifecycle (arrival → receiving → line receipt → confirm)
 *  so the goods land on stock in the dock buffer and a put-away task appears. The first two steps are
 *  idempotent — a delivery already arrived/receiving just no-ops (we swallow the 409). */
export const confirmReceipt = async (body: ConfirmReceiptBody): Promise<void> => {
  if (!current) return;
  const { deliveryId, lineNo, unit } = current;
  await api.post<void>(`logistics/deliveries/${deliveryId}/arrival`).catch(() => {});
  await api.post<void>(`logistics/deliveries/${deliveryId}/receiving`).catch(() => {});
  await api.post<void>(`logistics/deliveries/${deliveryId}/lines/${lineNo}/receipt`, {
    quantity: body.counted,
    unit,
    discrepancy: body.reason ? DISCREPANCY[body.reason] : 'None',
  });
  await api.post<void>(`logistics/deliveries/${deliveryId}/confirm`).catch(() => {});
};
