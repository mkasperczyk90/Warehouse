import { api } from '@/core/api/client';
import type { StatusKey } from '@/shared/theme/tokens';

/*
 * UC-10 — Picking, talking to the Logistics service directly: GET the order's pick list
 * (`logistics/orders/{id}/pick-list`) and confirm/short a task. The backend deals in codes (no
 * product names), so those fall back to codes. The terminal works one pending task at a time.
 */

const ORDER_ID = 'SO-4471';

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

/** The task the terminal is currently on (so confirm/short know which sequence to post). */
let currentSequence: number | null = null;

export const getPickStep = async (): Promise<PickStep> => {
  const pl = await api.get<PickListDto>(`logistics/orders/${ORDER_ID}/pick-list`);
  const task = pl.tasks.find((t) => t.status === 'Pending') ?? pl.tasks[0];
  currentSequence = task?.sequence ?? null;
  return {
    wave: pl.orderId,
    order: pl.orderId,
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
  api.post<void>(`logistics/orders/${ORDER_ID}/picks/${currentSequence}/confirm`);

/** Short pick → recorded (replanning onto another batch/location is the deferred wave optimiser). */
export const shortPick = (reason: ShortReason): Promise<void> =>
  api.post<void>(`logistics/orders/${ORDER_ID}/picks/${currentSequence}/short`, { reason });
