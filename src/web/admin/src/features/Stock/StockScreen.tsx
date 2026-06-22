import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from '@tanstack/react-router';
import { type ColumnDef } from '@tanstack/react-table';

import { DataTable, FilterBar, KpiCard, StatusBadge, type FilterPill } from '@/shared/ui';
import { useStockKpis, useStockRows, type StockRow } from './stock.model';
import styles from './StockScreen.module.css';

type PillKey = 'all' | 'coldRoom' | 'blocked' | 'expiring';

export function StockScreen() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const kpis = useStockKpis();
  const rows = useStockRows();

  const [search, setSearch] = useState('');
  const [pill, setPill] = useState<PillKey>('all');

  const pills: FilterPill[] = [
    { key: 'all', label: t('filter.all') },
    { key: 'coldRoom', label: t('filter.coldRoom') },
    { key: 'blocked', label: t('filter.blocked') },
    { key: 'expiring', label: t('filter.expiring') },
  ];

  const columns = useMemo<ColumnDef<StockRow, unknown>[]>(
    () => [
      {
        header: () => t('col.product'),
        accessorKey: 'product',
        cell: ({ row }) => (
          <div>
            <span>{row.original.product}</span>
            <div className={styles.sku}>{row.original.sku}</div>
          </div>
        ),
      },
      {
        id: 'batch',
        header: () => t('col.batchBbe'),
        accessorFn: (r) => `${r.batch} · ${r.bestBefore}`,
      },
      { header: () => t('col.location'), accessorKey: 'location' },
      { header: () => t('col.room'), accessorKey: 'room' },
      {
        header: () => t('col.onHand'),
        accessorKey: 'onHand',
        meta: { align: 'right' },
        cell: ({ getValue }) => (getValue() as number).toLocaleString(),
      },
      {
        header: () => t('col.atp'),
        accessorKey: 'atp',
        meta: { align: 'right' },
        cell: ({ getValue }) => (getValue() as number).toLocaleString(),
      },
      {
        header: () => t('col.status'),
        accessorKey: 'status',
        cell: ({ row }) => (
          <StatusBadge variant={row.original.status} label={row.original.statusLabel} />
        ),
      },
    ],
    [t],
  );

  const data = useMemo(() => {
    const all = rows.data ?? [];
    const q = search.trim().toLowerCase();
    return all.filter((r) => {
      const matchesPill =
        pill === 'all' ||
        (pill === 'coldRoom' && r.room === 'Cold room') ||
        (pill === 'blocked' && r.status === 'blocked') ||
        (pill === 'expiring' && r.status === 'expired');
      const matchesSearch =
        q === '' ||
        [r.product, r.sku, r.batch, r.location].some((f) => f.toLowerCase().includes(q));
      return matchesPill && matchesSearch;
    });
  }, [rows.data, search, pill]);

  return (
    <>
      <div className={styles.kpis}>
        {kpis.data ? (
          <>
            <KpiCard label={t('kpi.onHand')} value={kpis.data.onHand} unit={kpis.data.unit} />
            <KpiCard label={t('kpi.atp')} value={kpis.data.atp} unit={kpis.data.unit} />
            <KpiCard label={t('kpi.reserved')} value={kpis.data.reserved} unit={kpis.data.unit} />
            <KpiCard
              label={t('kpi.blockedExpiring')}
              value={kpis.data.blockedExpiring}
              unit={kpis.data.unit}
              warn
            />
          </>
        ) : null}
      </div>

      <div className={styles.panel}>
        <FilterBar
          searchPlaceholder={t('filter.searchStock')}
          search={search}
          onSearch={setSearch}
          pills={pills}
          activePill={pill}
          onPill={(k) => setPill(k as PillKey)}
        />

        {rows.isLoading ? (
          <p className={styles.state}>{t('state.loading')}</p>
        ) : rows.isError ? (
          <p className={styles.state}>{t('state.error')}</p>
        ) : data.length === 0 ? (
          <p className={styles.state}>{t('state.empty')}</p>
        ) : (
          <DataTable
            columns={columns}
            data={data}
            onRowClick={(r) => navigate({ to: '/stock/$id', params: { id: r.id } })}
          />
        )}
      </div>
    </>
  );
}
