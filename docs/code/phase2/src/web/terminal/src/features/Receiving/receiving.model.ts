import { api } from '@/core/api/client';

/** UC-02 — Goods receipt (one ASN line). */
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

export const getReceipt = (): Promise<Receipt> => api.get<Receipt>('asn/2206/line/3');

/** Why a counted quantity differs from the ASN (UC-02 · exceptions §Inbound). */
export type DiscrepancyReason = 'shortage' | 'overage' | 'damage';

export interface ConfirmReceiptBody {
  counted: number;
  /** A plain confirm, or a recorded discrepancy with its reason. */
  reason?: DiscrepancyReason;
}

/** Confirm one ASN line — receipt proceeds; a discrepancy is recorded, not blocked. */
export const confirmReceipt = (body: ConfirmReceiptBody): Promise<void> =>
  api.post<void>('asn/2206/line/3/confirm', body);
