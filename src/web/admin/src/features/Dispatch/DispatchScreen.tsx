import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus } from 'lucide-react';

import { Board, BoardColumn, Modal, StatusBadge } from '@/shared/ui';
import { humanRef } from '@/shared/format/ref';
import {
  CARRIERS,
  useAdvanceShipment,
  useAssignCarrier,
  useDispatchBoard,
  type Shipment,
} from './dispatch.model';
import styles from './DispatchScreen.module.css';

export function DispatchScreen() {
  const { t } = useTranslation();
  const board = useDispatchBoard();
  const assign = useAssignCarrier();
  const advance = useAdvanceShipment();

  const [pending, setPending] = useState<{ id: string; customer: string } | null>(null);
  const [carrier, setCarrier] = useState('');
  const [pickup, setPickup] = useState('');
  const [filter, setFilter] = useState('all');

  const openAssign = (ship: Shipment) => {
    setPending({ id: ship.id, customer: ship.customer });
    setCarrier('');
    setPickup('');
  };

  const confirm = () => {
    if (!pending || !carrier) return;
    assign.mutate(
      { id: pending.id, carrierCode: carrier, pickup: pickup.trim() || 'pickup TBC' },
      { onSuccess: () => setPending(null) },
    );
  };

  if (board.isLoading) return <p className={styles.state}>{t('state.loading')}</p>;
  if (board.isError || !board.data) return <p className={styles.state}>{t('state.error')}</p>;

  const matches = (s: Shipment) => filter === 'all' || s.carrier?.name === filter;

  return (
    <>
      <div className={styles.filterRow}>
        {['all', ...CARRIERS.map((c) => c.name)].map((c) => (
          <button
            key={c}
            type="button"
            className={`${styles.filterPill} ${filter === c ? styles.filterOn : ''}`}
            onClick={() => setFilter(c)}
          >
            {c === 'all' ? t('dispatch.filterAll') : c}
          </button>
        ))}
      </div>

      <Board>
        {board.data.map((col) => {
          const advanceLabel =
            col.key === 'assigned'
              ? t('dispatch.advance.notice')
              : col.key === 'noticeSent'
                ? t('dispatch.advance.collect')
                : undefined;
          const shipments = col.shipments.filter(matches);
          return (
            <BoardColumn
              key={col.key}
              title={t(`dispatch.col.${col.key}`)}
              count={shipments.length}
            >
              {shipments.map((ship) => (
                <ShipmentCard
                  key={ship.id}
                  ship={ship}
                  assignLabel={t('dispatch.assignCarrier')}
                  onAssign={() => openAssign(ship)}
                  advanceLabel={advanceLabel}
                  onAdvance={() => advance.mutate(ship.id)}
                  printLabel={t('dispatch.printWaybill')}
                />
              ))}
            </BoardColumn>
          );
        })}
      </Board>

      <Modal open={!!pending} title={t('dispatch.assign.title')} onClose={() => setPending(null)}>
        {pending ? (
          <>
            <p className={styles.dialogShip}>
              {humanRef('SHP', pending.id)} · {pending.customer}
            </p>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>{t('dispatch.assign.carrier')}</span>
              <select
                className={styles.input}
                value={carrier}
                aria-label={t('dispatch.assign.carrier')}
                onChange={(e) => setCarrier(e.target.value)}
              >
                <option value="">{t('dispatch.assign.selectCarrier')}</option>
                {CARRIERS.map((c) => (
                  <option key={c.code} value={c.code}>
                    {c.name}
                  </option>
                ))}
              </select>
            </label>
            <label className={styles.field}>
              <span className={styles.fieldLabel}>{t('dispatch.assign.pickup')}</span>
              <input
                className={styles.input}
                value={pickup}
                placeholder={t('dispatch.assign.pickupPh')}
                onChange={(e) => setPickup(e.target.value)}
              />
            </label>
            <div className={styles.dialogActions}>
              <button type="button" className={styles.ghost} onClick={() => setPending(null)}>
                {t('dispatch.assign.cancel')}
              </button>
              <button
                type="button"
                className={styles.primary}
                disabled={!carrier || assign.isPending}
                onClick={confirm}
              >
                {t('dispatch.assign.submit')}
              </button>
            </div>
          </>
        ) : null}
      </Modal>
    </>
  );
}

function ShipmentCard({
  ship,
  assignLabel,
  onAssign,
  advanceLabel,
  onAdvance,
  printLabel,
}: {
  ship: Shipment;
  assignLabel: string;
  onAssign: () => void;
  advanceLabel?: string;
  onAdvance: () => void;
  printLabel: string;
}) {
  return (
    <div className={styles.ship}>
      <div className={styles.id}>{humanRef('SHP', ship.id)}</div>
      <div className={styles.cust}>
        {ship.customer} · {ship.summary}
      </div>

      {(ship.carrier || ship.pickup || ship.badge) && (
        <div className={styles.row}>
          {ship.carrier ? (
            <span className={styles.car}>
              <span className={styles.dot}>{ship.carrier.code}</span> {ship.carrier.name}
            </span>
          ) : (
            <span />
          )}
          {ship.badge ? (
            <StatusBadge variant={ship.badge.variant} label={ship.badge.label} />
          ) : ship.pickup ? (
            <span>{ship.pickup}</span>
          ) : null}
        </div>
      )}

      {ship.tracking ? (
        <>
          <div className={styles.track}>↗ {ship.tracking}</div>
          <button type="button" className={styles.print} onClick={() => window.print()}>
            {printLabel}
          </button>
        </>
      ) : null}

      {ship.canAssign ? (
        <button type="button" className={styles.assign} onClick={onAssign}>
          <Plus size={14} aria-hidden /> {assignLabel}
        </button>
      ) : advanceLabel ? (
        <button type="button" className={styles.advance} onClick={onAdvance}>
          {advanceLabel} →
        </button>
      ) : null}
    </div>
  );
}
