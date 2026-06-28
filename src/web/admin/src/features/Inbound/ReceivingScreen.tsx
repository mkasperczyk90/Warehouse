import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft } from 'lucide-react';
import { useNavigate, useParams } from '@tanstack/react-router';
import { type ColumnDef } from '@tanstack/react-table';

import { DataTable, StatusBadge, type StatusVariant } from '@/shared/ui';
import { humanRef } from '@/shared/format/ref';
import { useReceiving, type ReceivingLine, type ReceivingLineStatus } from './inbound.model';
import styles from './ReceivingScreen.module.css';

const STATUS_VARIANT: Record<ReceivingLineStatus, StatusVariant> = {
  received: 'available',
  receiving: 'transit',
  pending: 'reserved',
};

/** Route component for `/inbound/$id/receiving`. */
export function ReceivingRoute() {
  const params = useParams({ strict: false });
  return <ReceivingScreen id={params.id} />;
}

/** UC-02 monitoring — receiving progress for an arrived ASN (read-only). */
export function ReceivingScreen({ id }: { id?: string }) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const receiving = useReceiving(id);

  const columns = useMemo<ColumnDef<ReceivingLine, unknown>[]>(
    () => [
      { header: () => t('inbound.receiving.col.sku'), accessorKey: 'sku' },
      { header: () => t('inbound.receiving.col.product'), accessorKey: 'product' },
      {
        header: () => t('inbound.receiving.col.expected'),
        accessorKey: 'expected',
        meta: { align: 'right' },
        cell: ({ getValue }) => (getValue() as number).toLocaleString(),
      },
      {
        header: () => t('inbound.receiving.col.received'),
        accessorKey: 'received',
        meta: { align: 'right' },
        cell: ({ getValue }) => (getValue() as number).toLocaleString(),
      },
      {
        header: () => t('inbound.receiving.col.status'),
        accessorKey: 'status',
        cell: ({ row }) => (
          <StatusBadge
            variant={STATUS_VARIANT[row.original.status]}
            label={row.original.statusLabel}
          />
        ),
      },
    ],
    [t],
  );

  if (receiving.isLoading) return <p className={styles.state}>{t('state.loading')}</p>;
  if (receiving.isError || !receiving.data)
    return <p className={styles.state}>{t('state.error')}</p>;

  const r = receiving.data;
  const percent = r.totalLines === 0 ? 0 : Math.round((r.receivedLines / r.totalLines) * 100);

  return (
    <>
      <button type="button" className={styles.back} onClick={() => navigate({ to: '/inbound' })}>
        <ArrowLeft size={16} aria-hidden /> {t('inbound.receiving.back')}
      </button>

      <div className={styles.head}>
        <h2 className={styles.title}>
          {t('inbound.receiving.title', { id: humanRef('ASN', r.id) })}
        </h2>
        <div className={styles.sub}>
          {r.supplier} · {r.dockSlot}
        </div>
      </div>

      <div className={styles.progress}>
        <div className={styles.bar}>
          <div className={styles.fill} style={{ width: `${percent}%` }} />
        </div>
        <div className={styles.progressText}>
          {t('inbound.receiving.progress', {
            received: r.receivedLines,
            total: r.totalLines,
          })}
        </div>
      </div>

      <div className={styles.panel}>
        <DataTable columns={columns} data={r.lines} />
      </div>
    </>
  );
}
