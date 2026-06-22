import { useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';
import type { StatusVariant } from '@/shared/ui';

/** The work-queue landing (admin-10-worklist) — "what needs me now". */
export type QueueKey = 'qc' | 'partial' | 'expiring' | 'inbound';

export interface WorklistCounts {
  qc: number;
  expiring: number;
  partial: number;
  inbound: number;
  stocktake: number;
}

export interface WorklistItem {
  id: string;
  label: string;
  sublabel: string;
  badge?: { variant: StatusVariant; label: string };
  meta?: string;
}

export interface WorklistQueue {
  key: QueueKey;
  count: number;
  /** Override the header count text, e.g. "top 4 of 12". */
  shownNote?: string;
  items: WorklistItem[];
}

export interface Worklist {
  counts: WorklistCounts;
  queues: WorklistQueue[];
}

export function useWorklist() {
  return useQuery({ queryKey: ['worklist'], queryFn: () => api.get<Worklist>('worklist') });
}
