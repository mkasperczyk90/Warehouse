import { api } from '@/core/api/client';

/*
 * UC-02 — Goods receipt (one delivery line), talking to the Logistics service directly:
 * GET/POST `logistics/deliveries/{id}/...`. The backend deals in codes (no supplier/product names,
 * which live in Partners/Catalog and aren't composed here), so those fields fall back to codes.
 */

/** The delivery + line this terminal is receiving (hardware would scan/route these in). */
const DELIVERY_ID = 'ASN-2206';
const LINE_NO = 3;

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
  const d = await api.get<DeliveryDto>(`logistics/deliveries/${DELIVERY_ID}`);
  const line = d.lines.find((l) => l.lineNo === LINE_NO) ?? d.lines[0];
  return {
    asn: d.id,
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

/** Confirm one delivery line — receipt proceeds; a discrepancy is recorded, not blocked. */
export const confirmReceipt = (body: ConfirmReceiptBody): Promise<void> =>
  api.post<void>(`logistics/deliveries/${DELIVERY_ID}/lines/${LINE_NO}/receipt`, {
    quantity: body.counted,
    unit: 'ea',
    discrepancy: body.reason ? DISCREPANCY[body.reason] : 'None',
  });
