import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { type ColumnDef } from '@tanstack/react-table';

import { DataTable, Modal, StatusBadge } from '@/shared/ui';
import {
  QC_REJECT_REASONS,
  QC_RELEASE_REASONS,
  useQcBatches,
  useQcDecision,
  type QcBatch,
  type QcDecision,
} from './qc.model';
import styles from './QualityScreen.module.css';

interface Pending {
  id: string;
  batch: string;
  product: string;
  decision: QcDecision;
}

export function QualityScreen() {
  const { t } = useTranslation();
  const batches = useQcBatches();
  const decide = useQcDecision();

  const [pending, setPending] = useState<Pending | null>(null);
  const [reason, setReason] = useState('');
  const [note, setNote] = useState('');

  const openDecision = useCallback((b: QcBatch, decision: QcDecision) => {
    setPending({ id: b.id, batch: b.batch, product: b.product, decision });
    setReason('');
    setNote('');
  }, []);

  const close = () => setPending(null);
  const confirm = () => {
    if (!pending || !reason) return;
    decide.mutate({ id: pending.id, decision: pending.decision, reason, note: note || undefined });
    close();
  };

  const columns = useMemo<ColumnDef<QcBatch, unknown>[]>(
    () => [
      {
        id: 'batchProduct',
        header: () => t('qc.col.batchProduct'),
        cell: ({ row }) => (
          <div>
            <span>
              {row.original.batch} · {row.original.product}
            </span>
            <div className={styles.sku}>{row.original.sku}</div>
          </div>
        ),
      },
      { header: () => t('qc.col.fromReceipt'), accessorKey: 'fromReceipt' },
      { header: () => t('col.location'), accessorKey: 'location' },
      {
        header: () => t('qc.col.qty'),
        accessorKey: 'qty',
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
      {
        id: 'decision',
        header: () => t('qc.col.decision'),
        cell: ({ row }) => (
          <div className={styles.acts}>
            <button
              type="button"
              className={styles.release}
              onClick={() => openDecision(row.original, 'release')}
            >
              {t('qc.release')}
            </button>
            <button
              type="button"
              className={styles.reject}
              onClick={() => openDecision(row.original, 'reject')}
            >
              {t('qc.reject')}
            </button>
          </div>
        ),
      },
    ],
    [t, openDecision],
  );

  const data = batches.data ?? [];
  const reasons = pending?.decision === 'reject' ? QC_REJECT_REASONS : QC_RELEASE_REASONS;

  return (
    <>
      <div className={styles.head}>
        <h2 className={styles.title}>{t('qc.title')}</h2>
        <div className={styles.sub}>{t('qc.subtitle')}</div>
      </div>

      <div className={styles.panel}>
        <div className={styles.panelHead}>
          {t('qc.panelTitle')} — {data.length}
        </div>
        {batches.isLoading ? (
          <p className={styles.state}>{t('state.loading')}</p>
        ) : batches.isError ? (
          <p className={styles.state}>{t('state.error')}</p>
        ) : data.length === 0 ? (
          <p className={styles.state}>{t('qc.cleared')}</p>
        ) : (
          <DataTable columns={columns} data={data} />
        )}
      </div>

      <Modal
        open={!!pending}
        title={pending ? t(`qc.confirm.${pending.decision}Title`) : ''}
        onClose={close}
      >
        {pending ? (
          <>
            <p className={styles.dialogText}>
              {pending.batch} · {pending.product}
            </p>
            <label className={styles.dialogField}>
              <span className={styles.dialogLabel}>{t('qc.reasonLabel')}</span>
              <select
                className={styles.dialogInput}
                aria-label={t('qc.reasonLabel')}
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              >
                <option value="">{t('qc.selectReason')}</option>
                {reasons.map((r) => (
                  <option key={r} value={r}>
                    {t(`qc.${pending.decision}Reason.${r}`)}
                  </option>
                ))}
              </select>
            </label>
            <label className={styles.dialogField}>
              <span className={styles.dialogLabel}>{t('qc.noteLabel')}</span>
              <input
                className={styles.dialogInput}
                value={note}
                onChange={(e) => setNote(e.target.value)}
              />
            </label>
            <div className={styles.dialogActions}>
              <button type="button" className={styles.ghost} onClick={close}>
                {t('qc.cancel')}
              </button>
              <button
                type="button"
                className={pending.decision === 'reject' ? styles.confirmReject : styles.confirmRelease}
                disabled={!reason}
                onClick={confirm}
              >
                {t(`qc.confirm.${pending.decision}`)}
              </button>
            </div>
          </>
        ) : null}
      </Modal>
    </>
  );
}
