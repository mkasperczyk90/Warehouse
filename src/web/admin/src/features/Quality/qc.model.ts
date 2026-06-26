import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { api } from '@/core/api/client';
import type { StatusVariant } from '@/shared/ui';

/** UC-03 — QC worklist (admin-8-qc). A batch held in quarantine. */
export interface QcBatch {
  id: string;
  batch: string;
  product: string;
  sku: string;
  fromReceipt: string;
  location: string;
  qty: number;
  unit: string;
  status: StatusVariant;
  statusLabel: string;
}

export type QcDecision = 'release' | 'reject';

/** Reasons recorded with a decision — every QC decision is audited (UC-03). */
export const QC_RELEASE_REASONS = ['passed', 'labOk', 'tempOk'] as const;
export const QC_REJECT_REASONS = ['failedTemp', 'damaged', 'expired', 'labFail'] as const;

const QC_KEY = ['qc', 'batches'];

export function useQcBatches() {
  return useQuery({
    queryKey: QC_KEY,
    queryFn: () => api.get<QcBatch[]>('inventory/qc/batches'),
  });
}

/**
 * Release/Reject with an optimistic update: the decided batch leaves the list
 * immediately (it's a hard quarantine, the inspector wants instant feedback),
 * and rolls back if the command fails. Settle re-syncs with the server.
 */
export function useQcDecision() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      decision,
      reason,
      note,
    }: {
      id: string;
      decision: QcDecision;
      reason: string;
      note?: string;
    }) => api.post(`inventory/qc/${id}/${decision}`, { reason, note }),
    onMutate: async ({ id }) => {
      await qc.cancelQueries({ queryKey: QC_KEY });
      const prev = qc.getQueryData<QcBatch[]>(QC_KEY);
      qc.setQueryData<QcBatch[]>(QC_KEY, (old) => old?.filter((b) => b.id !== id));
      return { prev };
    },
    onError: (_e, _v, ctx) => {
      if (ctx?.prev) qc.setQueryData(QC_KEY, ctx.prev);
    },
    onSettled: () => qc.invalidateQueries({ queryKey: QC_KEY }),
  });
}
