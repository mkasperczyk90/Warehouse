import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';
import { useNavigate } from '@tanstack/react-router';
import { type ColumnDef } from '@tanstack/react-table';

import { DataTable, Modal, StatusBadge, type StatusVariant } from '@/shared/ui';
import {
  useStartStocktake,
  useStocktakeList,
  type StocktakeListItem,
  type StocktakeState,
} from './stocktake.model';
import styles from './StocktakeListScreen.module.css';

const STATE_VARIANT: Record<StocktakeState, StatusVariant> = {
  scheduled: 'reserved',
  counting: 'transit',
  review: 'reserved',
  completed: 'available',
};

const SCOPES = ['Cold room 1, aisle A', 'Cold room 1, aisle B', 'Freezer 1', 'Standard hall A'];

export function StocktakeListScreen() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const list = useStocktakeList();
  const start = useStartStocktake();

  const [startOpen, setStartOpen] = useState(false);
  const [scope, setScope] = useState(SCOPES[0]);

  const onStart = () => {
    start.mutate(
      { scope },
      {
        onSuccess: (data) => {
          setStartOpen(false);
          navigate({ to: '/stocktake/$id', params: { id: data.id } });
        },
      },
    );
  };

  const columns = useMemo<ColumnDef<StocktakeListItem, unknown>[]>(
    () => [
      {
        id: 'scope',
        header: () => t('stocktake.col.scope'),
        cell: ({ row }) => (
          <div>
            <span>{row.original.scope}</span>
            <div className={styles.id}>{row.original.id}</div>
          </div>
        ),
      },
      {
        id: 'status',
        header: () => t('col.status'),
        cell: ({ row }) => (
          <StatusBadge
            variant={STATE_VARIANT[row.original.state]}
            label={t(`stocktake.state.${row.original.state}`)}
          />
        ),
      },
      {
        id: 'counted',
        header: () => t('stocktake.col.counted'),
        meta: { align: 'right' },
        cell: ({ row }) =>
          `${row.original.locationsCounted.toLocaleString()} / ${row.original.totalLocations.toLocaleString()}`,
      },
      {
        header: () => t('stocktake.card.discrepancies'),
        accessorKey: 'discrepancies',
        meta: { align: 'right' },
        cell: ({ getValue }) => (getValue() as number).toLocaleString(),
      },
    ],
    [t],
  );

  return (
    <>
      <div className={styles.head}>
        <div>
          <h2 className={styles.title}>{t('stocktake.listTitle')}</h2>
          <div className={styles.sub}>{t('stocktake.listSub')}</div>
        </div>
        <button type="button" className={styles.newBtn} onClick={() => setStartOpen(true)}>
          <Plus size={14} aria-hidden /> {t('stocktake.startCount')}
        </button>
      </div>

      <div className={styles.panel}>
        {list.isLoading ? (
          <p className={styles.state}>{t('state.loading')}</p>
        ) : list.isError ? (
          <p className={styles.state}>{t('state.error')}</p>
        ) : (
          <DataTable
            columns={columns}
            data={list.data ?? []}
            onRowClick={(s) => navigate({ to: '/stocktake/$id', params: { id: s.id } })}
          />
        )}
      </div>

      <Modal
        open={startOpen}
        title={t('stocktake.start.title')}
        onClose={() => setStartOpen(false)}
      >
        <label className={styles.field}>
          <span className={styles.fieldLabel}>{t('stocktake.start.scopeLabel')}</span>
          <select
            className={styles.input}
            value={scope}
            aria-label={t('stocktake.start.scopeLabel')}
            onChange={(e) => setScope(e.target.value)}
          >
            {SCOPES.map((s) => (
              <option key={s} value={s}>
                {s}
              </option>
            ))}
          </select>
        </label>
        <p className={styles.mode}>{t('stocktake.start.mode')}</p>
        <div className={styles.actions}>
          <button type="button" className={styles.ghost} onClick={() => setStartOpen(false)}>
            {t('stocktake.start.cancel')}
          </button>
          <button
            type="button"
            className={styles.primary}
            disabled={start.isPending}
            onClick={onStart}
          >
            {t('stocktake.start.start')}
          </button>
        </div>
      </Modal>
    </>
  );
}
