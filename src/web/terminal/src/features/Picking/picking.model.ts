import { api } from '@/core/api/client';
import type { StatusKey } from '@/shared/theme/tokens';
import { humanRef } from '@/shared/format/ref';

/*
 * UC-10 — Picking, talking to the Logistics service directly: find an order released to the floor
 * (status Picking), GET its pick list (`logistics/orders/{id}/pick-list`) and confirm/short a task.
 * The backend keys orders by GUID and deals in product codes (no names), so those fall back to codes
 * and a friendly ref. The terminal works one pending task at a time.
 */

interface OrderSummaryDto {
  id: string;
  status: string;
}

export interface PickStep {
  wave: string;
  order: string;
  picked: number;
  total: number;
  location: string;
  sku: string;
  product: string;
  fefo: string;
  fefoStatus: StatusKey;
  qty: number;
  unit: string;
}

interface PickTaskDto {
  sequence: number;
  location: string;
  productCode: string;
  batchNumber: string | null;
  quantity: number;
  unit: string;
  status: string;
}

interface PickListDto {
  orderId: string;
  picked: number;
  total: number;
  tasks: PickTaskDto[];
}

/** The order + task the terminal is currently on (so confirm/short know what to post). */
let currentOrderId: string | null = null;
let currentSequence: number | null = null;

export const getPickStep = async (): Promise<PickStep> => {
  // Pick the first order released to the floor (Picking) — its FEFO pick list is planned by Inventory.
  const orders = await api.get<OrderSummaryDto[]>('logistics/orders?status=Picking');
  currentOrderId = orders[0]?.id ?? null;
  if (!currentOrderId) {
    currentSequence = null;
    return {
      wave: '—', order: '—', picked: 0, total: 0, location: '—',
      sku: '—', product: '—', fefo: 'FEFO', fefoStatus: 'reserved', qty: 0, unit: 'ea',
    };
  }
  const pl = await api.get<PickListDto>(`logistics/orders/${currentOrderId}/pick-list`);
  const task = pl.tasks.find((t) => t.status === 'Pending') ?? pl.tasks[0];
  currentSequence = task?.sequence ?? null;
  return {
    wave: humanRef('WAVE', pl.orderId),
    order: humanRef('SO', pl.orderId),
    picked: pl.picked,
    total: pl.total,
    location: task?.location ?? '—',
    sku: task?.productCode ?? '—',
    product: task?.productCode ?? '—',
    fefo: task?.batchNumber ? `FEFO · ${task.batchNumber}` : 'FEFO',
    fefoStatus: 'reserved',
    qty: task?.quantity ?? 0,
    unit: task?.unit ?? 'ea',
  };
};

/** Why a pick fell short of the expected quantity (UC-10 · exceptions §Outbound). */
export type ShortReason = 'shortAtLocation' | 'batchBlocked' | 'damaged';

/** Confirm the pick (both scans landed) → hard allocation consumed in Inventory. */
export const confirmPick = (): Promise<void> =>
  api.post<void>(`logistics/orders/${currentOrderId}/picks/${currentSequence}/confirm`);

/** Short pick → recorded (replanning onto another batch/location is the deferred wave optimiser). */
export const shortPick = (reason: ShortReason): Promise<void> =>
  api.post<void>(`logistics/orders/${currentOrderId}/picks/${currentSequence}/short`, { reason });
