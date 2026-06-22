import { api } from '@/core/api/client';

export interface PackLine {
  name: string;
  lot: string;
  qty: string;
  done: boolean;
  remaining?: number;
}

/** UC-11 — Packing. */
export interface PackJob {
  order: string;
  customer: string;
  pkg: string;
  weight: string;
  dimensions: string;
  lines: PackLine[];
}

export const getPackJob = (): Promise<PackJob> => api.get<PackJob>('order/SO-4471/packing');

/** Close & label the current package — its contents/weight/dimensions are recorded. */
export const closePackage = (): Promise<void> => api.post<void>('order/SO-4471/packing/close');

/** Open another package for the remainder of the order. */
export const addPackage = (): Promise<void> => api.post<void>('order/SO-4471/packing/add');
