import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2 } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import { useNavigate, useSearch } from '@tanstack/react-router';
import { type ColumnDef } from '@tanstack/react-table';

import { DataTable, Modal, StatusBadge } from '@/shared/ui';
import { humanRef } from '@/shared/format/ref';
import {
  useCancelOrder,
  useCreateOrder,
  useDecideOrder,
  useOrderDetail,
  useOrderList,
  useReleaseOrder,
  useSkuStock,
  type NewOrderLine,
  type OrderDecision,
  type SoLine,
  type SoSummary,
} from './outbound.model';
import type { SelectionSearch } from '@/navigation/search';
import styles from './OutboundScreen.module.css';

const emptyLine = (): NewOrderLine => ({ sku: '', product: '', ordered: 0 });
const emptyForm = () => ({ customer: '', shipTo: '', requiredDate: '', lines: [emptyLine()] });

export function OutboundScreen() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const list = useOrderList();
  const create = useCreateOrder();
  // Selection lives in the URL (?selected=…) so a chosen order is deep-linkable and
  // refresh-safe; local state drives rendering between writes.
  const initial = useSearch({ strict: false }) as SelectionSearch;
  const [selected, setSelectedState] = useState<string | null>(initial.selected ?? null);
  const setSelected = (id: string | null) => {
    setSelectedState(id);
    navigate({ to: '/outbound', search: { selected: id ?? undefined }, replace: true });
  };
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState(emptyForm());

  const selectedId = selected ?? list.data?.[0]?.id ?? null;
  const detail = useOrderDetail(selectedId);
  const decide = useDecideOrder(selectedId);
  const release = useReleaseOrder(selectedId);
  const cancel = useCancelOrder(selectedId);

  const [atpSku, setAtpSku] = useState<string | null>(null);
  const skuStock = useSkuStock(atpSku);

  const invalidateOrder = () => {
    void queryClient.invalidateQueries({ queryKey: ['so', 'detail', selectedId] });
    void queryClient.invalidateQueries({ queryKey: ['so', 'list'] });
  };

  const onDecide = (decision: OrderDecision) =>
    decide.mutate({ decision }, { onSuccess: invalidateOrder });

  const onRelease = () => release.mutate(undefined, { onSuccess: invalidateOrder });
  const onCancel = () => cancel.mutate(undefined, { onSuccess: invalidateOrder });

  const setLine = (i: number, patch: Partial<NewOrderLine>) =>
    setForm((f) => ({
      ...f,
      lines: f.lines.map((l, idx) => (idx === i ? { ...l, ...patch } : l)),
    }));

  const validLines = form.lines.filter((l) => l.sku.trim() && l.ordered > 0);
  const canCreate = form.customer.trim() !== '' && validLines.length > 0;

  const submit = () =>
    create.mutate(
      {
        customer: form.customer.trim(),
        shipTo: form.shipTo.trim(),
        requiredDate: form.requiredDate,
        lines: validLines,
      },
      {
        onSuccess: (data) => {
          setCreateOpen(false);
          setForm(emptyForm());
          void queryClient.invalidateQueries({ queryKey: ['so', 'list'] });
          setSelected(data.id);
        },
      },
    );

  const columns = useMemo<ColumnDef<SoLine, unknown>[]>(
    () => [
      { header: () => t('col.sku'), accessorKey: 'sku' },
      { header: () => t('col.product'), accessorKey: 'product' },
      {
        header: () => t('col.ordered'),
        accessorKey: 'ordered',
        meta: { align: 'right' },
        cell: ({ getValue }) => (getValue() as number).toLocaleString(),
      },
      {
        header: () => t('col.atpAtOrder'),
        accessorKey: 'atpAtOrder',
        meta: { align: 'right' },
        cell: ({ getValue }) => (getValue() as number).toLocaleString(),
      },
      {
        header: () => t('col.reserved'),
        accessorKey: 'reserved',
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

  return (
    <div className={styles.split}>
      <div className={styles.list}>
        <div className={styles.listHead}>
          <b>{t('outbound.listTitle')}</b>
          <button type="button" className={styles.newBtn} onClick={() => setCreateOpen(true)}>
            <Plus size={14} aria-hidden /> {t('outbound.newOrder')}
          </button>
        </div>

        {list.isLoading ? (
          <p className={styles.state}>{t('state.loading')}</p>
        ) : list.isError ? (
          <p className={styles.state}>{t('state.error')}</p>
        ) : (
          (list.data ?? []).map((so: SoSummary) => (
            <button
              key={so.id}
              type="button"
              className={`${styles.so} ${so.id === selectedId ? styles.on : ''}`}
              onClick={() => setSelected(so.id)}
            >
              <span className={styles.soRow}>
                <span className={styles.soId}>{humanRef('SO', so.id)}</span>
                <StatusBadge variant={so.status} label={so.statusLabel} />
              </span>
              <span className={styles.soCust}>{so.customer}</span>
              <span className={styles.soMeta}>{so.meta}</span>
            </button>
          ))
        )}
      </div>

      <div className={styles.detail}>
        {detail.isLoading ? (
          <p className={styles.state}>{t('state.loading')}</p>
        ) : detail.isError || !detail.data ? (
          <p className={styles.state}>{t('state.error')}</p>
        ) : (
          <>
            <div className={styles.detailHead}>
              <div>
                <h2 className={styles.detailTitle}>
                  {humanRef('SO', detail.data.id)} — {detail.data.customer}
                </h2>
                <div className={styles.detailSub}>{detail.data.subtitle}</div>
              </div>
              <div className={styles.headRight}>
                <StatusBadge variant={detail.data.status} label={detail.data.statusLabel} />
                {detail.data.statusLabel === 'Partially reserved' ? (
                  <div className={styles.decideRow}>
                    <button
                      type="button"
                      className={styles.splitBtn}
                      disabled={decide.isPending}
                      onClick={() => onDecide('split')}
                    >
                      {t('outbound.decide.split')}
                    </button>
                    <button
                      type="button"
                      className={styles.holdBtn}
                      disabled={decide.isPending}
                      onClick={() => onDecide('hold')}
                    >
                      {t('outbound.decide.hold')}
                    </button>
                  </div>
                ) : null}
                {detail.data.statusLabel === 'Reserved' ? (
                  <button
                    type="button"
                    className={styles.splitBtn}
                    disabled={release.isPending}
                    onClick={onRelease}
                  >
                    {t('outbound.release')}
                  </button>
                ) : null}
                {!['Cancelled', 'Picking'].includes(detail.data.statusLabel) ? (
                  <button
                    type="button"
                    className={styles.cancelBtn}
                    disabled={cancel.isPending}
                    onClick={onCancel}
                  >
                    {t('outbound.cancel')}
                  </button>
                ) : null}
              </div>
            </div>

            <div className={styles.cards}>
              <Card label={t('outbound.card.linesReserved')} value={detail.data.linesReserved} />
              <Card
                label={t('outbound.card.reservedUnits')}
                value={detail.data.reservedUnits.toLocaleString()}
              />
              <Card label={t('outbound.card.shipTo')} value={detail.data.shipTo} small />
            </div>

            <div className={styles.panel}>
              <div className={styles.panelHead}>
                <span>{t('outbound.linesTitle')}</span>
                <small>{t('outbound.atpFormula')}</small>
              </div>
              <DataTable
                columns={columns}
                data={detail.data.lines}
                onRowClick={(line) => setAtpSku(line.sku)}
              />
              <div className={styles.note}>{t('outbound.note')}</div>
            </div>
          </>
        )}
      </div>

      <Modal
        open={createOpen}
        title={t('outbound.create.title')}
        onClose={() => setCreateOpen(false)}
      >
        <div className={styles.createGrid}>
          <label className={styles.createField}>
            <span className={styles.createLabel}>{t('outbound.create.customer')}</span>
            <input
              className={styles.input}
              value={form.customer}
              aria-label={t('outbound.create.customer')}
              onChange={(e) => setForm((f) => ({ ...f, customer: e.target.value }))}
            />
          </label>
          <label className={styles.createField}>
            <span className={styles.createLabel}>{t('outbound.create.shipTo')}</span>
            <input
              className={styles.input}
              value={form.shipTo}
              onChange={(e) => setForm((f) => ({ ...f, shipTo: e.target.value }))}
            />
          </label>
          <label className={styles.createField}>
            <span className={styles.createLabel}>{t('outbound.create.requiredDate')}</span>
            <input
              type="date"
              className={styles.input}
              value={form.requiredDate}
              onChange={(e) => setForm((f) => ({ ...f, requiredDate: e.target.value }))}
            />
          </label>
        </div>

        <div className={styles.linesLabel}>{t('outbound.create.lines')}</div>
        {form.lines.map((line, i) => (
          <div key={i} className={styles.lineRow}>
            <input
              className={styles.lineInput}
              placeholder={t('outbound.create.sku')}
              aria-label={`${t('outbound.create.sku')} ${i + 1}`}
              value={line.sku}
              onChange={(e) => setLine(i, { sku: e.target.value })}
            />
            <input
              className={styles.lineInput}
              placeholder={t('outbound.create.product')}
              value={line.product}
              onChange={(e) => setLine(i, { product: e.target.value })}
            />
            <input
              type="number"
              className={styles.lineQty}
              placeholder={t('outbound.create.qty')}
              aria-label={`${t('outbound.create.qty')} ${i + 1}`}
              value={line.ordered || ''}
              onChange={(e) => setLine(i, { ordered: Number(e.target.value) })}
            />
            <button
              type="button"
              className={styles.removeLine}
              aria-label="remove line"
              disabled={form.lines.length === 1}
              onClick={() =>
                setForm((f) => ({
                  ...f,
                  lines: f.lines.filter((_, idx) => idx !== i),
                }))
              }
            >
              <Trash2 size={14} aria-hidden />
            </button>
          </div>
        ))}
        <button
          type="button"
          className={styles.addLine}
          onClick={() => setForm((f) => ({ ...f, lines: [...f.lines, emptyLine()] }))}
        >
          <Plus size={14} aria-hidden /> {t('outbound.create.addLine')}
        </button>

        <div className={styles.createActions}>
          <button type="button" className={styles.ghost} onClick={() => setCreateOpen(false)}>
            {t('outbound.create.cancel')}
          </button>
          <button
            type="button"
            className={styles.primary}
            disabled={!canCreate || create.isPending}
            onClick={submit}
          >
            {t('outbound.create.submit')}
          </button>
        </div>
      </Modal>

      <Modal
        open={!!atpSku}
        title={t('outbound.atp.title', { sku: atpSku ?? '' })}
        onClose={() => setAtpSku(null)}
      >
        {skuStock.isLoading ? (
          <p className={styles.state}>{t('state.loading')}</p>
        ) : !skuStock.data || skuStock.data.length === 0 ? (
          <p className={styles.state}>{t('outbound.atp.none')}</p>
        ) : (
          <table className={styles.atpTable}>
            <thead>
              <tr>
                <th>{t('outbound.atp.location')}</th>
                <th>{t('outbound.atp.room')}</th>
                <th className={styles.atpNum}>{t('outbound.atp.onHand')}</th>
                <th className={styles.atpNum}>{t('outbound.atp.atp')}</th>
                <th>{t('outbound.atp.status')}</th>
              </tr>
            </thead>
            <tbody>
              {skuStock.data.map((r) => (
                <tr key={r.location}>
                  <td>{r.location}</td>
                  <td>{r.room}</td>
                  <td className={styles.atpNum}>{r.onHand.toLocaleString()}</td>
                  <td className={styles.atpNum}>{r.atp.toLocaleString()}</td>
                  <td>
                    <StatusBadge variant={r.status} label={r.statusLabel} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </Modal>
    </div>
  );
}

function Card({ label, value, small = false }: { label: string; value: string; small?: boolean }) {
  return (
    <div className={styles.card}>
      <div className={styles.cardLabel}>{label}</div>
      <div className={`${styles.cardValue} ${small ? styles.cardValueSmall : ''}`}>{value}</div>
    </div>
  );
}
