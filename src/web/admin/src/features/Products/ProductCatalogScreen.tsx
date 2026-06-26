import { useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Upload } from 'lucide-react';
import { useNavigate } from '@tanstack/react-router';
import { useQueryClient } from '@tanstack/react-query';
import { type ColumnDef } from '@tanstack/react-table';

import {
  DataTable,
  FilterBar,
  Modal,
  StatusBadge,
  type FilterPill,
  type StatusVariant,
} from '@/shared/ui';
import {
  IMPORT_COLUMNS,
  PRODUCT_CATEGORIES,
  PRODUCT_UNITS,
  STORAGE_MODES,
  defineProductSchema,
  parseProductsCsv,
  useDefineProduct,
  useImportProducts,
  useProductList,
  type DefineProductForm,
  type ImportResult,
  type ProductSummary,
  type StorageMode,
} from './product.model';
import styles from './ProductCatalogScreen.module.css';

/** Storage mode → badge colour (domain status, not decoration). */
const STORAGE_VARIANT: Record<StorageMode, StatusVariant> = {
  Ambient: 'available',
  ColdChain: 'reserved',
  Hazardous: 'blocked',
};

const EMPTY_DRAFT = {
  sku: '',
  name: '',
  ean: '',
  category: 'DryGoods',
  lengthCm: '10',
  widthCm: '10',
  heightCm: '10',
  unitWeightKg: '1',
  baseUnit: 'pcs',
  storage: 'Ambient',
  minCelsius: '',
  maxCelsius: '',
  isBatchTracked: false,
  hasExpiryDate: false,
};
type Draft = typeof EMPTY_DRAFT;

export function ProductCatalogScreen() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const list = useProductList();
  const define = useDefineProduct();
  const importProducts = useImportProducts();

  const [search, setSearch] = useState('');
  const [pill, setPill] = useState('all');
  const [open, setOpen] = useState(false);
  const [draft, setDraft] = useState<Draft>(EMPTY_DRAFT);
  const [error, setError] = useState<string | null>(null);
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  const [importError, setImportError] = useState<string | null>(null);
  const fileInput = useRef<HTMLInputElement>(null);

  async function onFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    e.target.value = ''; // allow re-selecting the same file
    if (!file) return;
    setImportError(null);
    const { rows, errors } = parseProductsCsv(await file.text());
    if (rows.length === 0 && errors.length === 0) {
      setImportError(t('products.import.empty'));
      return;
    }
    importProducts.mutate(rows, {
      onSuccess: (result) => {
        // Merge rows we rejected before the wire with the backend's per-row failures.
        setImportResult({ created: result.created, failed: [...errors, ...result.failed] });
        queryClient.invalidateQueries({ queryKey: ['products', 'list'] });
      },
      onError: () => setImportError(t('state.error')),
    });
  }

  const pills: FilterPill[] = [
    { key: 'all', label: t('filter.all') },
    ...PRODUCT_CATEGORIES.map((c) => ({ key: c, label: t(`products.categoryOpt.${c}`) })),
  ];

  const columns = useMemo<ColumnDef<ProductSummary, unknown>[]>(
    () => [
      {
        id: 'product',
        header: () => t('col.product'),
        cell: ({ row }) => (
          <div>
            <span>{row.original.name}</span>
            <div className={styles.sku}>{row.original.sku}</div>
          </div>
        ),
      },
      {
        header: () => t('products.category'),
        accessorKey: 'category',
        cell: ({ getValue }) => t(`products.categoryOpt.${getValue() as string}`),
      },
      {
        header: () => t('products.unit'),
        accessorKey: 'baseUnit',
        cell: ({ getValue }) => t(`products.unitOpt.${getValue() as string}`),
      },
      {
        id: 'storage',
        header: () => t('products.storageCol'),
        cell: ({ row }) => (
          <StatusBadge
            variant={STORAGE_VARIANT[row.original.storage]}
            label={t(`products.storageOpt.${row.original.storage}`)}
          />
        ),
      },
      {
        id: 'tracking',
        header: () => t('col.tracking'),
        cell: ({ row }) =>
          row.original.isBatchTracked ? (
            <span className={styles.chip}>{t('products.batchChip')}</span>
          ) : (
            <span className={styles.muted}>—</span>
          ),
      },
    ],
    [t],
  );

  const data = useMemo(() => {
    const all = list.data ?? [];
    const q = search.trim().toLowerCase();
    return all.filter((p) => {
      const matchesPill = pill === 'all' || p.category === pill;
      const matchesSearch = q === '' || [p.name, p.sku].some((f) => f.toLowerCase().includes(q));
      return matchesPill && matchesSearch;
    });
  }, [list.data, search, pill]);

  function submit() {
    setError(null);
    const parsed = defineProductSchema.safeParse({
      sku: draft.sku.trim(),
      name: draft.name.trim(),
      ean: draft.ean.trim() || undefined,
      category: draft.category,
      lengthCm: Number(draft.lengthCm),
      widthCm: Number(draft.widthCm),
      heightCm: Number(draft.heightCm),
      unitWeightKg: Number(draft.unitWeightKg),
      baseUnit: draft.baseUnit,
      storage: draft.storage,
      minCelsius: draft.minCelsius === '' ? null : Number(draft.minCelsius),
      maxCelsius: draft.maxCelsius === '' ? null : Number(draft.maxCelsius),
      isBatchTracked: draft.isBatchTracked,
      hasExpiryDate: draft.hasExpiryDate,
    } satisfies Record<keyof DefineProductForm, unknown>);

    if (!parsed.success) {
      setError(parsed.error.issues[0]?.message ?? t('state.error'));
      return;
    }

    define.mutate(parsed.data, {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ['products', 'list'] });
        setOpen(false);
        setDraft(EMPTY_DRAFT);
      },
    });
  }

  const set = <K extends keyof Draft>(key: K, value: Draft[K]) =>
    setDraft((d) => ({ ...d, [key]: value }));

  return (
    <>
      <div className={styles.head}>
        <div>
          <h2 className={styles.title}>{t('products.catalogTitle')}</h2>
          <div className={styles.sub}>{t('products.catalogSub')}</div>
        </div>
        <div className={styles.headActions}>
          <button
            type="button"
            className={styles.importBtn}
            disabled={importProducts.isPending}
            title={t('products.import.columnsHint', {
              columns: IMPORT_COLUMNS.join(', '),
            })}
            onClick={() => fileInput.current?.click()}
          >
            <Upload size={14} aria-hidden /> {t('products.import.button')}
          </button>
          <button type="button" className={styles.newBtn} onClick={() => setOpen(true)}>
            <Plus size={14} aria-hidden /> {t('products.define')}
          </button>
          <input
            ref={fileInput}
            type="file"
            accept=".csv,text/csv"
            className={styles.hiddenInput}
            aria-label={t('products.import.button')}
            onChange={onFile}
          />
        </div>
      </div>
      {importError ? <p className={styles.importError}>{importError}</p> : null}

      <div className={styles.panel}>
        <FilterBar
          searchPlaceholder={t('products.searchPlaceholder')}
          search={search}
          onSearch={setSearch}
          pills={pills}
          activePill={pill}
          onPill={setPill}
        />
        {list.isLoading ? (
          <p className={styles.state}>{t('state.loading')}</p>
        ) : list.isError ? (
          <p className={styles.state}>{t('state.error')}</p>
        ) : data.length === 0 ? (
          <p className={styles.state}>{t('state.empty')}</p>
        ) : (
          <DataTable
            columns={columns}
            data={data}
            onRowClick={(p) => navigate({ to: '/products/$sku', params: { sku: p.sku } })}
          />
        )}
      </div>

      <Modal open={open} title={t('products.defineTitle')} onClose={() => setOpen(false)}>
        <div className={styles.form}>
          <label className={styles.field}>
            <span>{t('products.f.sku')}</span>
            <input value={draft.sku} onChange={(e) => set('sku', e.target.value)} />
          </label>
          <label className={styles.field}>
            <span>{t('products.f.name')}</span>
            <input value={draft.name} onChange={(e) => set('name', e.target.value)} />
          </label>
          <label className={styles.field}>
            <span>{t('products.f.ean')}</span>
            <input value={draft.ean} onChange={(e) => set('ean', e.target.value)} />
          </label>
          <label className={styles.field}>
            <span>{t('products.f.category')}</span>
            <select value={draft.category} onChange={(e) => set('category', e.target.value)}>
              {PRODUCT_CATEGORIES.map((c) => (
                <option key={c} value={c}>
                  {t(`products.categoryOpt.${c}`)}
                </option>
              ))}
            </select>
          </label>
          <label className={styles.field}>
            <span>{t('products.f.baseUnit')}</span>
            <select value={draft.baseUnit} onChange={(e) => set('baseUnit', e.target.value)}>
              {PRODUCT_UNITS.map((u) => (
                <option key={u} value={u}>
                  {t(`products.unitOpt.${u}`)}
                </option>
              ))}
            </select>
          </label>

          <div className={styles.row3}>
            <label className={styles.field}>
              <span>{t('products.f.length')}</span>
              <input value={draft.lengthCm} onChange={(e) => set('lengthCm', e.target.value)} />
            </label>
            <label className={styles.field}>
              <span>{t('products.f.width')}</span>
              <input value={draft.widthCm} onChange={(e) => set('widthCm', e.target.value)} />
            </label>
            <label className={styles.field}>
              <span>{t('products.f.height')}</span>
              <input value={draft.heightCm} onChange={(e) => set('heightCm', e.target.value)} />
            </label>
          </div>

          <label className={styles.field}>
            <span>{t('products.f.weight')}</span>
            <input
              value={draft.unitWeightKg}
              onChange={(e) => set('unitWeightKg', e.target.value)}
            />
          </label>

          <label className={styles.field}>
            <span>{t('products.f.storage')}</span>
            <select value={draft.storage} onChange={(e) => set('storage', e.target.value)}>
              {STORAGE_MODES.map((m) => (
                <option key={m} value={m}>
                  {t(`products.storageOpt.${m}`)}
                </option>
              ))}
            </select>
          </label>
          {draft.storage !== 'Ambient' ? (
            <div className={styles.row3}>
              <label className={styles.field}>
                <span>{t('products.f.tempMin')}</span>
                <input
                  value={draft.minCelsius}
                  onChange={(e) => set('minCelsius', e.target.value)}
                />
              </label>
              <label className={styles.field}>
                <span>{t('products.f.tempMax')}</span>
                <input
                  value={draft.maxCelsius}
                  onChange={(e) => set('maxCelsius', e.target.value)}
                />
              </label>
            </div>
          ) : null}

          <div className={styles.checks}>
            <label className={styles.check}>
              <input
                type="checkbox"
                checked={draft.isBatchTracked}
                onChange={(e) => set('isBatchTracked', e.target.checked)}
              />
              {t('products.f.batchTracked')}
            </label>
            <label className={styles.check}>
              <input
                type="checkbox"
                checked={draft.hasExpiryDate}
                onChange={(e) => set('hasExpiryDate', e.target.checked)}
              />
              {t('products.f.expiryTracked')}
            </label>
          </div>

          {error ? <p className={styles.error}>{error}</p> : null}

          <div className={styles.actions}>
            <button type="button" className={styles.ghost} onClick={() => setOpen(false)}>
              {t('products.cancel')}
            </button>
            <button
              type="button"
              className={styles.primary}
              disabled={define.isPending}
              onClick={submit}
            >
              {t('products.create')}
            </button>
          </div>
        </div>
      </Modal>

      <Modal
        open={importResult !== null}
        title={t('products.import.resultTitle')}
        onClose={() => setImportResult(null)}
      >
        <div className={styles.summary}>
          <p className={styles.summaryLine}>
            {t('products.import.created', { n: importResult?.created ?? 0 })}
            {importResult && importResult.failed.length > 0
              ? ` · ${t('products.import.failed', { n: importResult.failed.length })}`
              : ''}
          </p>
          {importResult && importResult.failed.length > 0 ? (
            <ul className={styles.failList}>
              {importResult.failed.map((f, i) => (
                <li key={`${f.sku}-${i}`}>
                  <span className={styles.failSku}>{f.sku}</span> — {f.message}
                </li>
              ))}
            </ul>
          ) : null}
          <div className={styles.actions}>
            <button type="button" className={styles.primary} onClick={() => setImportResult(null)}>
              {t('products.import.done')}
            </button>
          </div>
        </div>
      </Modal>
    </>
  );
}
