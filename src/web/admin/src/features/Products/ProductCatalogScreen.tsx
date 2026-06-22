import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { useNavigate } from '@tanstack/react-router';
import { type ColumnDef } from '@tanstack/react-table';

import { DataTable, FilterBar, StatusBadge, type FilterPill } from '@/shared/ui';
import { PRODUCT_CATEGORIES, useProductList, type ProductSummary } from './product.model';
import styles from './ProductCatalogScreen.module.css';

/** Storage class derived from the temperature ceiling (display only). */
function storageClass(tempMax: number): 'frozen' | 'cold' | 'ambient' {
  if (tempMax <= -15) return 'frozen';
  if (tempMax <= 8) return 'cold';
  return 'ambient';
}

export function ProductCatalogScreen() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const list = useProductList();

  const [search, setSearch] = useState('');
  const [pill, setPill] = useState('all');

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
        accessorKey: 'unit',
        cell: ({ getValue }) => t(`products.unitOpt.${getValue() as string}`),
      },
      {
        id: 'storage',
        header: () => t('products.storageCol'),
        cell: ({ row }) => (
          <StatusBadge
            variant="reserved"
            label={t(`products.storageOpt.${storageClass(row.original.tempMax)}`)}
          />
        ),
      },
      {
        id: 'tracking',
        header: () => t('col.tracking'),
        cell: ({ row }) => (
          <span className={styles.chips}>
            {row.original.batchTracked ? (
              <span className={styles.chip}>{t('products.batchChip')}</span>
            ) : null}
            {row.original.expiryTracked ? (
              <span className={styles.chip}>{t('products.fefoChip')}</span>
            ) : null}
          </span>
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

  return (
    <>
      <div className={styles.head}>
        <div>
          <h2 className={styles.title}>{t('products.catalogTitle')}</h2>
          <div className={styles.sub}>{t('products.catalogSub')}</div>
        </div>
        <button
          type="button"
          className={styles.newBtn}
          onClick={() => navigate({ to: '/products/new' })}
        >
          <Plus size={14} aria-hidden /> {t('products.newProduct')}
        </button>
      </div>

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
    </>
  );
}
