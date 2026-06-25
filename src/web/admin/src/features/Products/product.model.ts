import { useMutation, useQuery } from '@tanstack/react-query';
import { z } from 'zod';

import { api } from '@/core/api/client';

/**
 * UC-13 — Product catalogue (master data). Types mirror the MasterData/Catalog backend contract
 * (the `catalog/products` resource behind the gateway), so going live is turning MSW off (ADR-0006).
 */
export const PRODUCT_CATEGORIES = [
  'DryGoods',
  'Refrigerated',
  'Frozen',
  'Hazardous',
  'Fragile',
  'BulkMaterial',
] as const;
export const PRODUCT_UNITS = ['pcs', 'kg', 'l', 'm3', 'plt', 'ctn'] as const;
export const STORAGE_MODES = ['Ambient', 'ColdChain', 'Hazardous'] as const;

export type ProductCategory = (typeof PRODUCT_CATEGORIES)[number];
export type ProductUnit = (typeof PRODUCT_UNITS)[number];
export type StorageMode = (typeof STORAGE_MODES)[number];

/** Row shape for the catalogue list (backend `ProductSummaryDto`). */
export interface ProductSummary {
  sku: string;
  name: string;
  category: ProductCategory;
  baseUnit: ProductUnit;
  storage: StorageMode;
  isBatchTracked: boolean;
}

export interface Dimensions {
  lengthCm: number;
  widthCm: number;
  heightCm: number;
}

export interface Storage {
  mode: StorageMode;
  minCelsius: number | null;
  maxCelsius: number | null;
  requiresColdChain: boolean;
  isHazardous: boolean;
}

export interface UnitConversion {
  unit: ProductUnit;
  factorToBase: number;
}

/** Full product card (backend `ProductDto`). */
export interface Product {
  sku: string;
  name: string;
  ean: string | null;
  category: ProductCategory;
  dimensions: Dimensions;
  unitWeightKg: number;
  baseUnit: ProductUnit;
  storage: Storage;
  isBatchTracked: boolean;
  hasExpiryDate: boolean;
  unitConversions: UnitConversion[];
}

const RESOURCE = 'catalog/products';

// --- Define (create) — mirrors DefineProductCommand --------------------------
export const defineProductSchema = z
  .object({
    sku: z.string().regex(/^[A-Za-z0-9][A-Za-z0-9-]{1,31}$/, 'SKU: 2–32 chars, A–Z, 0–9, “-”'),
    name: z.string().min(1, 'A name is required'),
    ean: z.string().optional(),
    category: z.enum(PRODUCT_CATEGORIES),
    lengthCm: z.number().positive('Must be > 0'),
    widthCm: z.number().positive('Must be > 0'),
    heightCm: z.number().positive('Must be > 0'),
    unitWeightKg: z.number().min(0, 'Cannot be negative'),
    baseUnit: z.enum(PRODUCT_UNITS),
    storage: z.enum(STORAGE_MODES),
    minCelsius: z.number().nullable(),
    maxCelsius: z.number().nullable(),
    isBatchTracked: z.boolean(),
    hasExpiryDate: z.boolean(),
  })
  // The temperature range is required for a cold chain and, when present, must be valid; expiry
  // dates only make sense when batches are tracked. These mirror the aggregate's invariants.
  .refine((d) => d.storage !== 'ColdChain' || (d.minCelsius != null && d.maxCelsius != null), {
    path: ['minCelsius'],
    message: 'Cold chain needs a temperature range',
  })
  .refine((d) => d.minCelsius == null || d.maxCelsius == null || d.minCelsius <= d.maxCelsius, {
    path: ['maxCelsius'],
    message: 'Max temperature must be ≥ min',
  })
  .refine((d) => !d.hasExpiryDate || d.isBatchTracked, {
    path: ['hasExpiryDate'],
    message: 'Expiry dates require batch tracking',
  });

export type DefineProductForm = z.infer<typeof defineProductSchema>;

export interface ChangeStorageBody {
  storage: StorageMode;
  minCelsius: number | null;
  maxCelsius: number | null;
}

export function useProductList(category?: ProductCategory | 'all') {
  return useQuery({
    queryKey: ['products', 'list', category ?? 'all'],
    queryFn: () =>
      api.get<ProductSummary[]>(
        category && category !== 'all' ? `${RESOURCE}?category=${category}` : RESOURCE,
      ),
  });
}

export function useProduct(sku: string | undefined) {
  return useQuery({
    queryKey: ['products', 'detail', sku],
    queryFn: () => api.get<Product>(`${RESOURCE}/${sku}`),
    enabled: !!sku,
  });
}

export function useDefineProduct() {
  return useMutation({
    mutationFn: (body: DefineProductForm) => api.post(RESOURCE, body),
  });
}

export function useRenameProduct(sku: string) {
  return useMutation({
    mutationFn: (name: string) => api.post(`${RESOURCE}/${sku}/rename`, { name }),
  });
}

export function useChangeStorage(sku: string) {
  return useMutation({
    mutationFn: (body: ChangeStorageBody) => api.post(`${RESOURCE}/${sku}/storage`, body),
  });
}

// --- Bulk import (CSV) — mirrors ImportProductsCommand/Result ----------------

/** One rejected row, as the backend reports it (stable `code`, human `message`). */
export interface ImportRowError {
  sku: string;
  code: string;
  message: string;
}

/** Backend `ImportProductsResult`: how many landed and which rows failed. */
export interface ImportResult {
  created: number;
  failed: ImportRowError[];
}

/** Outcome of parsing a CSV file client-side: well-formed rows ready to POST, plus rows we could not
 * even shape (missing/!numeric required fields) so they never reach the wire and break the whole batch. */
export interface ParsedCsv {
  rows: DefineProductForm[];
  errors: ImportRowError[];
}

/** The header columns the CSV import expects (order-independent; matched case-insensitively by name). */
export const IMPORT_COLUMNS = [
  'sku',
  'name',
  'ean',
  'category',
  'lengthCm',
  'widthCm',
  'heightCm',
  'unitWeightKg',
  'baseUnit',
  'storage',
  'minCelsius',
  'maxCelsius',
  'isBatchTracked',
  'hasExpiryDate',
] as const;

const TRUTHY = new Set(['true', '1', 'yes', 'y']);

/** Split one CSV line, honouring double-quoted fields (so a name may contain a comma). */
function splitCsvLine(line: string): string[] {
  const out: string[] = [];
  let cur = '';
  let inQuotes = false;
  for (let i = 0; i < line.length; i++) {
    const ch = line[i];
    if (inQuotes) {
      if (ch === '"' && line[i + 1] === '"') {
        cur += '"';
        i++;
      } else if (ch === '"') {
        inQuotes = false;
      } else {
        cur += ch;
      }
    } else if (ch === '"') {
      inQuotes = true;
    } else if (ch === ',') {
      out.push(cur);
      cur = '';
    } else {
      cur += ch;
    }
  }
  out.push(cur);
  return out.map((s) => s.trim());
}

/** Parse a products CSV (header row required) into rows + locally-rejected rows. Domain validity
 * (categories, cold-chain temperature rules, duplicates) is left to the backend, which reports it per row. */
export function parseProductsCsv(text: string): ParsedCsv {
  const lines = text.split(/\r?\n/).filter((l) => l.trim() !== '');
  const rows: DefineProductForm[] = [];
  const errors: ImportRowError[] = [];
  if (lines.length < 2) return { rows, errors };

  const header = splitCsvLine(lines[0]).map((h) => h.toLowerCase());
  const col = (name: string) => header.indexOf(name.toLowerCase());

  for (let i = 1; i < lines.length; i++) {
    const cells = splitCsvLine(lines[i]);
    const get = (name: string) => {
      const j = col(name);
      return j >= 0 ? (cells[j] ?? '') : '';
    };
    const num = (name: string): number | null => {
      const raw = get(name);
      if (raw === '') return null;
      const n = Number(raw);
      return Number.isNaN(n) ? NaN : n;
    };

    const sku = get('sku');
    const lengthCm = num('lengthCm');
    const widthCm = num('widthCm');
    const heightCm = num('heightCm');
    const unitWeightKg = num('unitWeightKg');
    const minCelsius = num('minCelsius');
    const maxCelsius = num('maxCelsius');

    const numbersBad =
      [lengthCm, widthCm, heightCm, unitWeightKg].some((v) => v === null || Number.isNaN(v)) ||
      Number.isNaN(minCelsius) ||
      Number.isNaN(maxCelsius);

    if (numbersBad) {
      errors.push({ sku: sku || `#${i}`, code: 'csv_invalid_number', message: `row ${i}` });
      continue;
    }

    rows.push({
      sku,
      name: get('name'),
      ean: get('ean') || undefined,
      category: get('category') as ProductCategory,
      lengthCm: lengthCm as number,
      widthCm: widthCm as number,
      heightCm: heightCm as number,
      unitWeightKg: unitWeightKg as number,
      baseUnit: get('baseUnit') as ProductUnit,
      storage: get('storage') as StorageMode,
      minCelsius,
      maxCelsius,
      isBatchTracked: TRUTHY.has(get('isBatchTracked').toLowerCase()),
      hasExpiryDate: TRUTHY.has(get('hasExpiryDate').toLowerCase()),
    });
  }

  return { rows, errors };
}

export function useImportProducts() {
  return useMutation({
    mutationFn: (rows: DefineProductForm[]) =>
      api.post<ImportResult>(`${RESOURCE}/import`, { products: rows }),
  });
}
