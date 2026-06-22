import { useMutation, useQuery } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/core/api/client';

/** UC-13 — Product master data (admin-4-product). */
export const PRODUCT_CATEGORIES = ['dairy', 'frozen', 'produce', 'dry', 'packaging'] as const;
export const PRODUCT_UNITS = ['ea', 'kg', 'l', 'case'] as const;

export const productSchema = z
  .object({
    sku: z.string().min(8, 'SKU must be at least 8 characters'),
    name: z.string().min(1, 'A name is required'),
    ean: z.string().min(8, 'EAN looks too short'),
    category: z.enum(PRODUCT_CATEGORIES),
    unit: z.enum(PRODUCT_UNITS),
    length: z.number().min(0),
    width: z.number().min(0),
    height: z.number().min(0),
    weight: z.number().min(0),
    packConversion: z.string(),
    tempMin: z.number(),
    tempMax: z.number(),
    hazardous: z.boolean(),
    batchTracked: z.boolean(),
    expiryTracked: z.boolean(),
  })
  // Storage requirements drive the put-away invariant — the range must be valid.
  .refine((d) => d.tempMin <= d.tempMax, {
    path: ['tempMax'],
    message: 'Max temperature must be ≥ min',
  });

export type ProductForm = z.infer<typeof productSchema>;

export interface ProductDraft extends ProductForm {
  lastEdited: string;
}

/** Row shape for the catalogue list. */
export interface ProductSummary {
  sku: string;
  name: string;
  category: ProductForm['category'];
  unit: ProductForm['unit'];
  tempMin: number;
  tempMax: number;
  batchTracked: boolean;
  expiryTracked: boolean;
}

export function useProductList() {
  return useQuery({
    queryKey: ['products', 'list'],
    queryFn: () => api.get<ProductSummary[]>('products'),
  });
}

export function useProduct(sku: string | undefined) {
  return useQuery({
    queryKey: ['products', 'detail', sku],
    queryFn: () => api.get<ProductDraft>(`products/${sku}`),
    enabled: !!sku,
  });
}

export function useSaveProduct() {
  return useMutation({
    mutationFn: (body: ProductForm) => api.post('products', body),
  });
}
