import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ArrowLeft, ArrowLeftRight, Ban, SlidersHorizontal } from 'lucide-react';
import { useNavigate, useParams } from '@tanstack/react-router';
import { useQueryClient } from '@tanstack/react-query';
import { type ColumnDef } from '@tanstack/react-table';

import { DataTable, KpiCard, Modal, StatusBadge } from '@/shared/ui';
import {
  BLOCK_REASONS,
  useBlockStock,
  useLocations,
  useMoveStock,
  useStockItem,
  type BlockReason,
  type StockMovementRow,
} from './stock.model';
import styles from './StockItemScreen.module.css';

/** Temperature class of a room, for the move compatibility check. */
function roomClass(room: string): string {
  const r = room.toLowerCase();
  if (r.includes('freez')) return 'freezer';
  if (r.includes('cold')) return 'cold';
  return 'standard';
}

/** Route component for `/stock/$id`. */
export function StockItemRoute() {
  const params = useParams({ strict: false });
  return <StockItemScreen id={params.id} />;
}

/** UC-05 drill-down — one stock item: breakdown, movements, and row actions. */
export function StockItemScreen({ id }: { id?: string }) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const item = useStockItem(id);
  const locations = useLocations().data ?? [];

  const move = useMoveStock(id);
  const block = useBlockStock(id);

  const [action, setAction] = useState<'move' | 'block' | null>(null);
  const [moveTo, setMoveTo] = useState('');
  const [blockReason, setBlockReason] = useState<BlockReason | ''>('');
  const [blockNote, setBlockNote] = useState('');
  const [banner, setBanner] = useState<string | null>(null);

  const close = () => setAction(null);
  const afterWrite = (message: string) => {
    setBanner(message);
    setAction(null);
    void queryClient.invalidateQueries({ queryKey: ['stock', 'item', id] });
    void queryClient.invalidateQueries({ queryKey: ['stock', 'rows'] });
  };

  const columns = useMemo<ColumnDef<StockMovementRow, unknown>[]>(
    () => [
      { header: () => t('stockItem.date'), accessorKey: 'date' },
      { header: () => t('stockItem.type'), accessorKey: 'type' },
      {
        header: () => t('stockItem.qty'),
        accessorKey: 'qty',
        meta: { align: 'right' },
        cell: ({ getValue }) => {
          const n = getValue() as number;
          return `${n > 0 ? '+' : ''}${n.toLocaleString()}`;
        },
      },
      { header: () => t('stockItem.reference'), accessorKey: 'reference' },
    ],
    [t],
  );

  if (item.isLoading) return <p className={styles.state}>{t('state.loading')}</p>;
  if (item.isError || !item.data) return <p className={styles.state}>{t('state.error')}</p>;

  const d = item.data;
  const target = locations.find((l) => l.address === moveTo);
  const compatible = !!target && roomClass(d.room) === target.roomType;

  return (
    <>
      <button type="button" className={styles.back} onClick={() => navigate({ to: '/stock' })}>
        <ArrowLeft size={16} aria-hidden /> {t('stockItem.back')}
      </button>

      <div className={styles.head}>
        <div>
          <h2 className={styles.title}>
            {d.product} · {d.batch}
          </h2>
          <div className={styles.sub}>
            SKU {d.sku} · {d.location} · {d.room} · {d.bestBefore}
          </div>
        </div>
        <div className={styles.headRight}>
          <StatusBadge variant={d.status} label={d.statusLabel} />
          <button
            type="button"
            className={styles.ghostAct}
            onClick={() => {
              setMoveTo('');
              setAction('move');
            }}
          >
            <ArrowLeftRight size={14} aria-hidden /> {t('stockItem.move')}
          </button>
          <button
            type="button"
            className={styles.dangerAct}
            onClick={() => {
              setBlockReason('');
              setBlockNote('');
              setAction('block');
            }}
          >
            <Ban size={14} aria-hidden /> {t('stockItem.block')}
          </button>
          <button
            type="button"
            className={styles.adjust}
            onClick={() => navigate({ to: '/adjustment/$itemId', params: { itemId: d.id } })}
          >
            <SlidersHorizontal size={14} aria-hidden /> {t('stockItem.adjust')}
          </button>
        </div>
      </div>

      {banner ? <div className={styles.banner}>{banner}</div> : null}

      <div className={styles.kpis}>
        <KpiCard label={t('kpi.onHand')} value={d.onHand} unit={d.unit} />
        <KpiCard label={t('kpi.atp')} value={d.atp} unit={d.unit} />
        <KpiCard label={t('kpi.reserved')} value={d.reserved} unit={d.unit} />
      </div>

      <div className={styles.panel}>
        <div className={styles.panelHead}>{t('stockItem.movements')}</div>
        <DataTable columns={columns} data={d.movements} />
      </div>

      {/* --- Move dialog: enforces the environment-compatibility invariant --- */}
      <Modal open={action === 'move'} title={t('stockItem.moveTitle')} onClose={close}>
        <label className={styles.field}>
          <span className={styles.fieldLabel}>{t('stockItem.moveTo')}</span>
          <select
            className={styles.input}
            value={moveTo}
            aria-label={t('stockItem.moveTo')}
            onChange={(e) => setMoveTo(e.target.value)}
          >
            <option value="">{t('stockItem.selectLocation')}</option>
            {locations.map((l) => (
              <option key={l.address} value={l.address}>
                {l.address} · {l.room}
              </option>
            ))}
          </select>
        </label>

        {moveTo ? (
          <div className={compatible ? styles.compatOk : styles.compatBad}>
            {compatible
              ? t('stockItem.compatOk')
              : t('stockItem.compatBad', { from: d.room, to: target?.room ?? '' })}
          </div>
        ) : null}

        <div className={styles.dialogActions}>
          <button type="button" className={styles.ghost} onClick={close}>
            {t('stockItem.cancel')}
          </button>
          <button
            type="button"
            className={styles.primary}
            disabled={!compatible || move.isPending}
            onClick={() =>
              move.mutate(
                { toLocation: moveTo },
                { onSuccess: () => afterWrite(t('stockItem.moved')) },
              )
            }
          >
            {t('stockItem.moveConfirm')}
          </button>
        </div>
      </Modal>

      {/* --- Block dialog: reason-bearing quarantine --- */}
      <Modal open={action === 'block'} title={t('stockItem.blockTitle')} onClose={close}>
        <label className={styles.field}>
          <span className={styles.fieldLabel}>{t('stockItem.blockReason')}</span>
          <select
            className={styles.input}
            value={blockReason}
            aria-label={t('stockItem.blockReason')}
            onChange={(e) => setBlockReason(e.target.value as BlockReason | '')}
          >
            <option value="">{t('stockItem.selectReason')}</option>
            {BLOCK_REASONS.map((r) => (
              <option key={r} value={r}>
                {t(`stockItem.blockReasonOpt.${r}`)}
              </option>
            ))}
          </select>
        </label>
        <label className={styles.field}>
          <span className={styles.fieldLabel}>{t('stockItem.blockNote')}</span>
          <input
            className={styles.input}
            value={blockNote}
            onChange={(e) => setBlockNote(e.target.value)}
          />
        </label>

        <div className={styles.dialogActions}>
          <button type="button" className={styles.ghost} onClick={close}>
            {t('stockItem.cancel')}
          </button>
          <button
            type="button"
            className={styles.danger}
            disabled={!blockReason || block.isPending}
            onClick={() =>
              block.mutate(
                {
                  reason: blockReason as BlockReason,
                  note: blockNote || undefined,
                },
                { onSuccess: () => afterWrite(t('stockItem.blocked')) },
              )
            }
          >
            {t('stockItem.blockConfirm')}
          </button>
        </div>
      </Modal>
    </>
  );
}
