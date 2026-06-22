import { api } from '@/core/api/client';
import type { StatusKey } from '@/shared/theme/tokens';

/** UC-10 — Picking step. */
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

export const getPickStep = (): Promise<PickStep> => api.get<PickStep>('wave/W-2206/next');

/** Why a pick fell short of the expected quantity (UC-10 · exceptions §Outbound). */
export type ShortReason = 'shortAtLocation' | 'batchBlocked' | 'damaged';

/** Confirm the pick (both scans landed) → hard allocation consumed. */
export const confirmPick = (): Promise<void> => api.post<void>('wave/W-2206/next/confirm');

/** Short pick → replan onto the next FEFO-eligible batch/location. */
export const shortPick = (reason: ShortReason): Promise<void> =>
  api.post<void>('wave/W-2206/next/short', { reason });
