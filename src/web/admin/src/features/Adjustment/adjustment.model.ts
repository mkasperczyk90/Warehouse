import { useMutation, useQuery } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/core/api/client';
import type { StatusVariant } from '@/shared/ui';

/** UC-08 — Stock adjustment (admin-9-adjustment). The item being corrected. */
export interface AdjustmentDraft {
  itemId: string;
  product: string;
  batch: string;
  sku: string;
  location: string;
  room: string;
  status: StatusVariant;
  statusLabel: string;
  systemOnHand: number;
  unit: string;
}

export const ADJUSTMENT_REASONS = ['damage', 'loss', 'correction', 'found'] as const;
export type AdjustmentReason = (typeof ADJUSTMENT_REASONS)[number];

/**
 * Invariant #3 — stock can never be driven below zero. The form refuses a
 * negative counted quantity before it ever reaches the ledger.
 */
export const adjustmentSchema = z.object({
  newQuantity: z
    .number({ message: 'A counted quantity is required' })
    .int('Whole units only')
    .min(0, 'Quantity can never go below zero (invariant #3)'),
  reason: z.enum(ADJUSTMENT_REASONS, { message: 'A reason is required' }),
  note: z.string().max(280).optional(),
});
export type AdjustmentForm = z.infer<typeof adjustmentSchema>;

export interface AdjustmentResult {
  itemId: string;
  newOnHand: number;
  delta: number;
  reason: string;
  postedBy: string;
  postedAt: string;
}

export function useAdjustmentDraft(itemId?: string) {
  return useQuery({
    queryKey: ['adjustment', 'draft', itemId ?? 'default'],
    queryFn: () =>
      api.get<AdjustmentDraft>(
        itemId ? `inventory/adjustments/draft/${itemId}` : 'inventory/adjustments/draft',
      ),
  });
}

export function usePostAdjustment() {
  return useMutation({
    mutationFn: (body: AdjustmentForm & { itemId: string }) =>
      api.post<AdjustmentResult>('inventory/adjustments', body),
  });
}
