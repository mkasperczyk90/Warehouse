import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type ColumnDef } from '@tanstack/react-table';

import { DataTable, FilterBar, type FilterPill } from '@/shared/ui';
import { MOVEMENT_TYPES, useMovements, type MovementRow } from './movements.model';
import styles from './MovementsScreen.module.css';

export function MovementsScreen() {
  const { t } = useTranslation();
  const movements = useMovements();

  const [search, setSearch] = useState('');
  const [type, setType] = useState('all');

  const pills: FilterPill[] = [
    { key: 'all', label: t('filter.all') },
    ...MOVEMENT_TYPES.map((m) => ({
      key: m,
      label: t(`movements.type.${m}`),
    })),
  ];

  const columns = useMemo<ColumnDef<MovementRow, unknown>[]>(
    () => [
      { header: () => t('movements.col.date'), accessorKey: 'date' },
      {
        header: () => t('movements.col.type'),
        accessorKey: 'type',
        cell: ({ row }) => <span className={styles.typeChip}>{row.original.typeLabel}</span>,
      },
      {
        header: () => t('col.product'),
        accessorKey: 'product',
        cell: ({ row }) => (
          <div>
            <span>{row.original.product}</span>
            <div className={styles.sub}>
              {row.original.sku} · {row.original.batch}
            </div>
          </div>
        ),
      },
      { header: () => t('col.location'), accessorKey: 'location' },
      {
        header: () => t('movements.col.qty'),
        accessorKey: 'qty',
        meta: { align: 'right' },
        cell: ({ row }) => (
          <span className={row.original.qty < 0 ? styles.neg : styles.pos}>
            {row.original.qty > 0 ? '+' : ''}
            {row.original.qty.toLocaleString()} {row.original.unit}
          </span>
        ),
      },
      {
        header: () => t('movements.col.reference'),
        accessorKey: 'reference',
      },
    ],
    [t],
  );

  const data = useMemo(() => {
    const all = movements.data ?? [];
    const q = search.trim().toLowerCase();
    return all.filter((m) => {
      const matchesType = type === 'all' || m.type === type;
      const matchesSearch =
        q === '' ||
        [m.product, m.sku, m.location, m.reference].some((f) => f.toLowerCase().includes(q));
      return matchesType && matchesSearch;
    });
  }, [movements.data, search, type]);

  return (
    <>
      <div className={styles.head}>
        <h2 className={styles.title}>{t('movements.title')}</h2>
        <div className={styles.headSub}>{t('movements.sub')}</div>
      </div>

      <div className={styles.panel}>
        <FilterBar
          searchPlaceholder={t('movements.searchPlaceholder')}
          search={search}
          onSearch={setSearch}
          pills={pills}
          activePill={type}
          onPill={setType}
        />
        {movements.isLoading ? (
          <p className={styles.state}>{t('state.loading')}</p>
        ) : movements.isError ? (
          <p className={styles.state}>{t('state.error')}</p>
        ) : data.length === 0 ? (
          <p className={styles.state}>{t('state.empty')}</p>
        ) : (
          <DataTable columns={columns} data={data} />
        )}
      </div>
    </>
  );
}
