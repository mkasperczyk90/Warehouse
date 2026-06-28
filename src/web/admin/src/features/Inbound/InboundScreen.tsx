import { useCallback, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { CalendarClock, ClipboardList, Plus, Trash2 } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import { useNavigate, useSearch } from '@tanstack/react-router';
import { type ColumnDef } from '@tanstack/react-table';

import { DataTable, Modal, StatusBadge } from '@/shared/ui';
import { humanRef } from '@/shared/format/ref';
import {
  DOCKS,
  useAsnDetail,
  useAsnList,
  useAssignDock,
  useCreateAsn,
  useMarkArrived,
  useResolveSku,
  type AsnLine,
  type AsnSummary,
  type NewAsnLine,
} from './inbound.model';
import { useWarehouses, warehouseLabel } from '@/features/Warehouses';
import type { SelectionSearch } from '@/navigation/search';
import styles from './InboundScreen.module.css';

const UNITS = ['ea', 'kg', 'l', 'case'];
const emptyLine = (): NewAsnLine => ({ sku: '', product: '', planned: 0, unit: 'ea' });
// warehouse is filled from the live site list (see useWarehouses) once the create modal renders.
const emptyForm = () => ({ supplier: '', warehouse: '', dockSlot: '', lines: [emptyLine()] });

export function InboundScreen() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const list = useAsnList();
  const create = useCreateAsn();
  const warehouses = useWarehouses();
  // Selection lives in the URL (?selected=…) so a chosen ASN is deep-linkable and
  // refresh-safe; local state drives rendering between writes.
  const initial = useSearch({ strict: false }) as SelectionSearch;
  const [selected, setSelectedState] = useState<string | null>(initial.selected ?? null);
  const setSelected = (id: string | null) => {
    setSelectedState(id);
    navigate({ to: '/inbound', search: { selected: id ?? undefined }, replace: true });
  };
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState(emptyForm());
  // The site list owns the codes (WH01, …) the backend expects; default to the first one.
  const warehouseOptions = warehouses.data ?? [];
  const selectedWarehouse = form.warehouse || warehouseOptions[0]?.code || '';

  // Default to the first ASN until the coordinator picks another.
  const selectedId = selected ?? list.data?.[0]?.id ?? null;
  const detail = useAsnDetail(selectedId);
  const assignDock = useAssignDock(selectedId);
  const markArrived = useMarkArrived(selectedId);
  const [dockOpen, setDockOpen] = useState(false);
  const [dockForm, setDockForm] = useState({ dock: DOCKS[0], window: '' });

  const onArrive = () =>
    markArrived.mutate(undefined, {
      onSuccess: () => {
        void queryClient.invalidateQueries({ queryKey: ['asn', 'detail', selectedId] });
        void queryClient.invalidateQueries({ queryKey: ['asn', 'list'] });
      },
    });

  const submitDock = () =>
    assignDock.mutate(
      { dock: dockForm.dock, window: dockForm.window.trim() },
      {
        onSuccess: () => {
          setDockOpen(false);
          void queryClient.invalidateQueries({ queryKey: ['asn', 'detail', selectedId] });
          void queryClient.invalidateQueries({ queryKey: ['asn', 'list'] });
        },
      },
    );

  const setLine = (i: number, patch: Partial<NewAsnLine>) =>
    setForm((f) => ({
      ...f,
      lines: f.lines.map((l, idx) => (idx === i ? { ...l, ...patch } : l)),
    }));

  const validLines = form.lines.filter((l) => l.sku.trim() && l.planned > 0);
  const canCreate =
    form.supplier.trim() !== '' && selectedWarehouse !== '' && validLines.length > 0;

  const submit = () =>
    create.mutate(
      {
        supplier: form.supplier.trim(),
        warehouse: selectedWarehouse,
        dockSlot: form.dockSlot.trim() || 'slot pending',
        lines: validLines,
      },
      {
        onSuccess: (data) => {
          setCreateOpen(false);
          setForm(emptyForm());
          void queryClient.invalidateQueries({ queryKey: ['asn', 'list'] });
          setSelected(data.id);
        },
      },
    );

  const resolveSku = useResolveSku(selectedId);
  const [resolveLine, setResolveLine] = useState<AsnLine | null>(null);
  const [resolveForm, setResolveForm] = useState({ sku: '', product: '', create: false });
  const openResolve = useCallback((line: AsnLine) => {
    setResolveLine(line);
    setResolveForm({ sku: '', product: '', create: false });
  }, []);
  const canResolve = resolveForm.sku.trim() !== '' && resolveForm.product.trim() !== '';
  const submitResolve = () => {
    if (!resolveLine) return;
    resolveSku.mutate(
      {
        lineId: resolveLine.id,
        sku: resolveForm.sku.trim(),
        product: resolveForm.product.trim(),
        create: resolveForm.create,
      },
      {
        onSuccess: () => {
          setResolveLine(null);
          void queryClient.invalidateQueries({ queryKey: ['asn', 'detail', selectedId] });
        },
      },
    );
  };

  const columns = useMemo<ColumnDef<AsnLine, unknown>[]>(
    () => [
      { header: () => t('col.sku'), accessorKey: 'sku' },
      {
        header: () => t('col.product'),
        accessorKey: 'product',
        cell: ({ row }) =>
          row.original.flagged ? (
            <span className={styles.warn}>{row.original.product}</span>
          ) : (
            row.original.product
          ),
      },
      {
        header: () => t('col.planned'),
        accessorKey: 'planned',
        meta: { align: 'right' },
        cell: ({ getValue }) => (getValue() as number).toLocaleString(),
      },
      { header: () => t('col.unit'), accessorKey: 'unit', meta: { align: 'right' } },
      { header: () => t('col.tracking'), accessorKey: 'tracking' },
      {
        id: 'action',
        header: () => '',
        cell: ({ row }) =>
          row.original.flagged ? (
            <button
              type="button"
              className={styles.resolveBtn}
              onClick={() => openResolve(row.original)}
            >
              {t('inbound.resolve.action')}
            </button>
          ) : null,
      },
    ],
    [t, openResolve],
  );

  return (
    <div className={styles.split}>
      <div className={styles.list}>
        <div className={styles.listHead}>
          <b>{t('inbound.listTitle')}</b>
          <button type="button" className={styles.newBtn} onClick={() => setCreateOpen(true)}>
            <Plus size={14} aria-hidden /> {t('inbound.newAsn')}
          </button>
        </div>

        {list.isLoading ? (
          <p className={styles.state}>{t('state.loading')}</p>
        ) : list.isError ? (
          <p className={styles.state}>{t('state.error')}</p>
        ) : (
          (list.data ?? []).map((asn: AsnSummary) => (
            <button
              key={asn.id}
              type="button"
              className={`${styles.asn} ${asn.id === selectedId ? styles.on : ''}`}
              onClick={() => setSelected(asn.id)}
            >
              <span className={styles.asnRow}>
                <span className={styles.asnId}>{humanRef('ASN', asn.id)}</span>
                <StatusBadge variant={asn.status} label={asn.statusLabel} />
              </span>
              <span className={styles.asnSup}>{asn.supplier}</span>
              <span className={styles.asnMeta}>{asn.meta}</span>
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
                  {humanRef('ASN', detail.data.id)} — {detail.data.supplier}
                </h2>
                <div className={styles.detailSub}>{detail.data.createdBy}</div>
              </div>
              <div className={styles.headRight}>
                <StatusBadge variant={detail.data.status} label={detail.data.statusLabel} />
                {detail.data.status !== 'available' ? (
                  <button
                    type="button"
                    className={styles.slotBtn}
                    onClick={() => {
                      setDockForm({ dock: DOCKS[0], window: '' });
                      setDockOpen(true);
                    }}
                  >
                    <CalendarClock size={14} aria-hidden /> {t('inbound.dock.assign')}
                  </button>
                ) : null}
                {detail.data.statusLabel === 'Announced' ? (
                  <button
                    type="button"
                    className={styles.slotBtn}
                    disabled={markArrived.isPending}
                    onClick={onArrive}
                  >
                    {t('inbound.arrive')}
                  </button>
                ) : null}
                {detail.data.status === 'transit' ? (
                  <button
                    type="button"
                    className={styles.slotBtn}
                    onClick={() =>
                      navigate({
                        to: '/inbound/$id/receiving',
                        params: { id: detail.data.id },
                      })
                    }
                  >
                    <ClipboardList size={14} aria-hidden /> {t('inbound.receiving.view')}
                  </button>
                ) : null}
              </div>
            </div>

            <div className={styles.fields}>
              <Field label={t('inbound.field.supplier')} value={detail.data.supplier} />
              <Field label={t('inbound.field.warehouse')} value={detail.data.warehouse} />
              <Field label={t('inbound.field.dock')} value={detail.data.dockSlot} />
            </div>

            <div className={styles.panel}>
              <div className={styles.panelHead}>
                {t('inbound.linesTitle')} ({detail.data.lines.length})
              </div>
              <DataTable columns={columns} data={detail.data.lines} />
            </div>
          </>
        )}
      </div>

      <Modal
        open={createOpen}
        title={t('inbound.create.title')}
        onClose={() => setCreateOpen(false)}
      >
        <div className={styles.createGrid}>
          <label className={styles.createField}>
            <span className={styles.createLabel}>{t('inbound.create.supplier')}</span>
            <input
              className={styles.input}
              value={form.supplier}
              aria-label={t('inbound.create.supplier')}
              onChange={(e) => setForm((f) => ({ ...f, supplier: e.target.value }))}
            />
          </label>
          <label className={styles.createField}>
            <span className={styles.createLabel}>{t('inbound.create.warehouse')}</span>
            <select
              className={styles.input}
              value={selectedWarehouse}
              onChange={(e) => setForm((f) => ({ ...f, warehouse: e.target.value }))}
            >
              {warehouseOptions.map((w) => (
                <option key={w.id} value={w.code}>
                  {warehouseLabel(w)}
                </option>
              ))}
            </select>
          </label>
          <label className={styles.createField}>
            <span className={styles.createLabel}>{t('inbound.create.dock')}</span>
            <input
              className={styles.input}
              value={form.dockSlot}
              placeholder={t('inbound.create.dockPh')}
              onChange={(e) => setForm((f) => ({ ...f, dockSlot: e.target.value }))}
            />
          </label>
        </div>

        <div className={styles.linesLabel}>{t('inbound.create.lines')}</div>
        {form.lines.map((line, i) => (
          <div key={i} className={styles.lineRow}>
            <input
              className={styles.lineInput}
              placeholder={t('inbound.create.sku')}
              aria-label={`${t('inbound.create.sku')} ${i + 1}`}
              value={line.sku}
              onChange={(e) => setLine(i, { sku: e.target.value })}
            />
            <input
              className={styles.lineInput}
              placeholder={t('inbound.create.product')}
              value={line.product}
              onChange={(e) => setLine(i, { product: e.target.value })}
            />
            <input
              type="number"
              className={styles.lineQty}
              placeholder={t('inbound.create.qty')}
              aria-label={`${t('inbound.create.qty')} ${i + 1}`}
              value={line.planned || ''}
              onChange={(e) => setLine(i, { planned: Number(e.target.value) })}
            />
            <select
              className={styles.lineUnit}
              value={line.unit}
              onChange={(e) => setLine(i, { unit: e.target.value })}
            >
              {UNITS.map((u) => (
                <option key={u} value={u}>
                  {u}
                </option>
              ))}
            </select>
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
          <Plus size={14} aria-hidden /> {t('inbound.create.addLine')}
        </button>

        <div className={styles.createActions}>
          <button type="button" className={styles.ghost} onClick={() => setCreateOpen(false)}>
            {t('inbound.create.cancel')}
          </button>
          <button
            type="button"
            className={styles.primary}
            disabled={!canCreate || create.isPending}
            onClick={submit}
          >
            {t('inbound.create.submit')}
          </button>
        </div>
      </Modal>

      <Modal open={dockOpen} title={t('inbound.dock.title')} onClose={() => setDockOpen(false)}>
        <label className={styles.dockField}>
          <span className={styles.createLabel}>{t('inbound.dock.dock')}</span>
          <select
            className={styles.input}
            value={dockForm.dock}
            aria-label={t('inbound.dock.dock')}
            onChange={(e) => setDockForm((f) => ({ ...f, dock: e.target.value }))}
          >
            {DOCKS.map((d) => (
              <option key={d} value={d}>
                {d}
              </option>
            ))}
          </select>
        </label>
        <label className={styles.dockField}>
          <span className={styles.createLabel}>{t('inbound.dock.window')}</span>
          <input
            className={styles.input}
            value={dockForm.window}
            placeholder={t('inbound.dock.windowPh')}
            aria-label={t('inbound.dock.window')}
            onChange={(e) => setDockForm((f) => ({ ...f, window: e.target.value }))}
          />
        </label>
        <div className={styles.createActions}>
          <button type="button" className={styles.ghost} onClick={() => setDockOpen(false)}>
            {t('inbound.dock.cancel')}
          </button>
          <button
            type="button"
            className={styles.primary}
            disabled={assignDock.isPending}
            onClick={submitDock}
          >
            {t('inbound.dock.submit')}
          </button>
        </div>
      </Modal>

      <Modal
        open={!!resolveLine}
        title={t('inbound.resolve.title')}
        onClose={() => setResolveLine(null)}
      >
        {resolveLine ? (
          <>
            <p className={styles.resolveHint}>{resolveLine.product}</p>
            <label className={styles.dockField}>
              <span className={styles.createLabel}>{t('inbound.resolve.sku')}</span>
              <input
                className={styles.input}
                value={resolveForm.sku}
                aria-label={t('inbound.resolve.sku')}
                onChange={(e) => setResolveForm((f) => ({ ...f, sku: e.target.value }))}
              />
            </label>
            <label className={styles.dockField}>
              <span className={styles.createLabel}>{t('inbound.resolve.product')}</span>
              <input
                className={styles.input}
                value={resolveForm.product}
                aria-label={t('inbound.resolve.product')}
                onChange={(e) => setResolveForm((f) => ({ ...f, product: e.target.value }))}
              />
            </label>
            <label className={styles.resolveCheck}>
              <input
                type="checkbox"
                checked={resolveForm.create}
                aria-label={t('inbound.resolve.create')}
                onChange={(e) => setResolveForm((f) => ({ ...f, create: e.target.checked }))}
              />
              {t('inbound.resolve.create')}
            </label>
            <div className={styles.createActions}>
              <button type="button" className={styles.ghost} onClick={() => setResolveLine(null)}>
                {t('inbound.resolve.cancel')}
              </button>
              <button
                type="button"
                className={styles.primary}
                disabled={!canResolve || resolveSku.isPending}
                onClick={submitResolve}
              >
                {t('inbound.resolve.submit')}
              </button>
            </div>
          </>
        ) : null}
      </Modal>
    </div>
  );
}

function Field({ label, value }: { label: string; value: string }) {
  return (
    <div className={styles.field}>
      <div className={styles.fieldLabel}>{label}</div>
      <div className={styles.fieldValue}>{value}</div>
    </div>
  );
}
