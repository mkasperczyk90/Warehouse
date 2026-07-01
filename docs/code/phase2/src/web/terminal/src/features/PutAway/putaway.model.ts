import { api } from '@/core/api/client';

/** UC-04 — Put-away proposal. */
export interface PutAwayTask {
  task: number;
  ofTasks: number;
  lpn: string;
  product: string;
  chips: string[];
  coldChip: string;
  location: string;
  why: string;
  checks: string[];
}

export const getPutAwayTask = (): Promise<PutAwayTask> => api.get<PutAwayTask>('putaway/next');

/** Confirm the pallet into the proposed location → ledger PutAway movement. */
export const confirmPutAway = (): Promise<void> => api.post<void>('putaway/next/confirm');

/** Location full/over capacity → ask the system for the next legal bay (Invariant #2). */
export const proposeAnotherBay = (): Promise<void> => api.post<void>('putaway/next/propose');
