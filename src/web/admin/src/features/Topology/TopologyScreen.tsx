import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { useNavigate, useSearch } from '@tanstack/react-router';
import {
  AlertTriangle,
  ArrowDownToLine,
  Grid3x3,
  Plus,
  Snowflake,
  Square,
  Thermometer,
  Warehouse,
  type LucideIcon,
} from 'lucide-react';

import { Modal, StatusBadge } from '@/shared/ui';
import {
  useAddLocation,
  useAddRoom,
  useRoom,
  useSaveLocation,
  useSaveRoom,
  useTopologyTree,
  type LocationRow,
  type RoomType,
  type TopologyNode,
} from './topology.model';
import type { SelectionSearch } from '@/navigation/search';
import styles from './TopologyScreen.module.css';

const ICONS: Record<string, LucideIcon> = {
  warehouse: Warehouse,
  cold: Snowflake,
  freezer: Snowflake,
  standard: Square,
  hazmat: AlertTriangle,
  dock: ArrowDownToLine,
  location: Grid3x3,
};

const ROOM_TYPES: RoomType[] = ['cold', 'freezer', 'standard', 'hazmat', 'dock'];
const emptyRoom = () => ({
  code: '',
  warehouse: '',
  type: 'cold' as RoomType,
  tempMin: 2,
  tempMax: 6,
});

export function TopologyScreen() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const tree = useTopologyTree();
  // The selected room lives in the URL (?selected=…) so a chosen room is
  // deep-linkable and refresh-safe; local state drives rendering between writes.
  const initial = useSearch({ strict: false }) as SelectionSearch;
  const [selected, setSelectedState] = useState<string | null>(initial.selected ?? null);
  const setSelected = (id: string | null) => {
    setSelectedState(id);
    navigate({ to: '/topology', search: { selected: id ?? undefined }, replace: true });
  };

  const firstRoom = useMemo(
    () => tree.data?.find((n) => n.kind === 'room')?.id ?? null,
    [tree.data],
  );
  const warehouseOptions = useMemo(
    () =>
      (tree.data ?? [])
        .filter((n) => n.kind === 'warehouse')
        .map((n) => ({ id: n.id, label: n.label })),
    [tree.data],
  );
  const selectedId = selected ?? firstRoom;
  const room = useRoom(selectedId);
  const save = useSaveRoom(selectedId ?? undefined);
  const saveLocation = useSaveLocation(selectedId ?? undefined);
  const addLocation = useAddLocation(selectedId ?? undefined);
  const addRoom = useAddRoom();
  const queryClient = useQueryClient();

  // Editable room fields, seeded when the room loads.
  const [form, setForm] = useState<{ type: RoomType; tempMin: number; tempMax: number } | null>(
    null,
  );
  useEffect(() => {
    if (room.data)
      setForm({
        type: room.data.type,
        tempMin: room.data.tempMin,
        tempMax: room.data.tempMax,
      });
  }, [room.data]);

  const [editLoc, setEditLoc] = useState<LocationRow | null>(null);
  const [editForm, setEditForm] = useState({ capacity: 0, loadLimit: 0 });
  const [addOpen, setAddOpen] = useState(false);
  const [addForm, setAddForm] = useState({ address: '', capacity: 0, loadLimit: 0 });
  const [roomOpen, setRoomOpen] = useState(false);
  const [roomForm, setRoomForm] = useState(emptyRoom());

  const submitAddRoom = () => {
    addRoom.mutate(
      { ...roomForm, code: roomForm.code.trim() },
      {
        onSuccess: (data) => {
          setRoomOpen(false);
          void queryClient.invalidateQueries({ queryKey: ['topology', 'tree'] });
          setSelected(data.id);
        },
      },
    );
  };

  const invalidateRoom = () =>
    void queryClient.invalidateQueries({ queryKey: ['topology', 'room', selectedId] });

  const openEdit = (loc: LocationRow) => {
    setEditLoc(loc);
    setEditForm({ capacity: loc.capacity, loadLimit: loc.loadLimit });
  };
  const submitEdit = () => {
    if (!editLoc) return;
    saveLocation.mutate(
      { id: editLoc.id, capacity: editForm.capacity, loadLimit: editForm.loadLimit },
      {
        onSuccess: () => {
          setEditLoc(null);
          invalidateRoom();
        },
      },
    );
  };
  const submitAdd = () => {
    addLocation.mutate(
      {
        address: addForm.address.trim(),
        capacity: addForm.capacity,
        loadLimit: addForm.loadLimit,
      },
      {
        onSuccess: () => {
          setAddOpen(false);
          invalidateRoom();
        },
      },
    );
  };

  return (
    <div className={styles.split}>
      <div className={styles.tree}>
        <div className={styles.treeHead}>
          <button
            type="button"
            className={styles.addRoom}
            onClick={() => {
              setRoomForm({
                ...emptyRoom(),
                warehouse: warehouseOptions[0]?.id ?? '',
              });
              setRoomOpen(true);
            }}
          >
            <Plus size={14} aria-hidden /> {t('topology.addRoom.action')}
          </button>
        </div>
        {(tree.data ?? []).map((node: TopologyNode) => {
          const Icon = ICONS[node.icon] ?? Square;
          const isRoom = node.kind === 'room';
          return (
            <button
              key={node.id}
              type="button"
              disabled={!isRoom}
              className={`${styles.node} ${styles[`l${node.level}`]} ${
                node.id === selectedId ? styles.on : ''
              }`}
              onClick={() => isRoom && setSelected(node.id)}
            >
              <Icon size={16} className={styles.nodeIcon} aria-hidden />
              <span>{node.label}</span>
              {node.tag ? (
                <span className={styles.tag}>
                  <StatusBadge variant="reserved" label={node.tag} />
                </span>
              ) : null}
            </button>
          );
        })}
      </div>

      <Modal open={roomOpen} title={t('topology.addRoom.title')} onClose={() => setRoomOpen(false)}>
        <label className={styles.field}>
          <span className={styles.fieldLabel}>{t('topology.addRoom.code')}</span>
          <input
            className={styles.input}
            value={roomForm.code}
            aria-label={t('topology.addRoom.code')}
            onChange={(e) => setRoomForm((f) => ({ ...f, code: e.target.value }))}
          />
        </label>
        <label className={styles.field}>
          <span className={styles.fieldLabel}>{t('topology.addRoom.warehouse')}</span>
          <select
            className={styles.input}
            value={roomForm.warehouse}
            aria-label={t('topology.addRoom.warehouse')}
            onChange={(e) => setRoomForm((f) => ({ ...f, warehouse: e.target.value }))}
          >
            {warehouseOptions.map((w) => (
              <option key={w.id} value={w.id}>
                {w.label}
              </option>
            ))}
          </select>
        </label>
        <label className={styles.field}>
          <span className={styles.fieldLabel}>{t('topology.type')}</span>
          <select
            className={styles.input}
            value={roomForm.type}
            aria-label={t('topology.type')}
            onChange={(e) => setRoomForm((f) => ({ ...f, type: e.target.value as RoomType }))}
          >
            {ROOM_TYPES.map((rt) => (
              <option key={rt} value={rt}>
                {t(`topology.typeOpt.${rt}`)}
              </option>
            ))}
          </select>
        </label>
        <div className={styles.grid}>
          <label className={styles.field}>
            <span className={styles.fieldLabel}>{t('topology.tempMin')}</span>
            <input
              type="number"
              className={styles.input}
              value={roomForm.tempMin}
              aria-label={t('topology.tempMin')}
              onChange={(e) => setRoomForm((f) => ({ ...f, tempMin: Number(e.target.value) }))}
            />
          </label>
          <label className={styles.field}>
            <span className={styles.fieldLabel}>{t('topology.tempMax')}</span>
            <input
              type="number"
              className={styles.input}
              value={roomForm.tempMax}
              aria-label={t('topology.tempMax')}
              onChange={(e) => setRoomForm((f) => ({ ...f, tempMax: Number(e.target.value) }))}
            />
          </label>
        </div>
        <div className={styles.dialogActions}>
          <button type="button" className={styles.ghost} onClick={() => setRoomOpen(false)}>
            {t('topology.cancel')}
          </button>
          <button
            type="button"
            className={styles.primary}
            disabled={!roomForm.code.trim() || addRoom.isPending}
            onClick={submitAddRoom}
          >
            {t('topology.addRoom.submit')}
          </button>
        </div>
      </Modal>

      <div className={styles.detail}>
        {room.isLoading || !form ? (
          <p className={styles.state}>{t('state.loading')}</p>
        ) : room.isError || !room.data ? (
          <p className={styles.state}>{t('state.error')}</p>
        ) : (
          <>
            <div className={styles.detailHead}>
              <div>
                <h2 className={styles.detailTitle}>
                  {room.data.name} — {room.data.warehouse}
                </h2>
                <div className={styles.detailSub}>{t('topology.roomSub')}</div>
              </div>
              <div className={styles.actions}>
                <button type="button" className={styles.ghost}>
                  {t('topology.cancel')}
                </button>
                <button
                  type="button"
                  className={styles.primary}
                  disabled={save.isPending || save.isSuccess}
                  onClick={() => save.mutate(form)}
                >
                  {save.isSuccess ? t('topology.saved') : t('topology.save')}
                </button>
              </div>
            </div>

            <section className={styles.card}>
              <h3 className={styles.cardTitle}>{t('topology.room')}</h3>
              <div className={styles.grid}>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>{t('topology.type')}</span>
                  <select
                    className={styles.input}
                    value={form.type}
                    aria-label={t('topology.type')}
                    onChange={(e) => setForm({ ...form, type: e.target.value as RoomType })}
                  >
                    {ROOM_TYPES.map((rt) => (
                      <option key={rt} value={rt}>
                        {t(`topology.typeOpt.${rt}`)}
                      </option>
                    ))}
                  </select>
                </label>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>{t('topology.tempMin')}</span>
                  <input
                    type="number"
                    className={styles.input}
                    value={form.tempMin}
                    aria-label={t('topology.tempMin')}
                    onChange={(e) => setForm({ ...form, tempMin: Number(e.target.value) })}
                  />
                </label>
                <label className={styles.field}>
                  <span className={styles.fieldLabel}>{t('topology.tempMax')}</span>
                  <input
                    type="number"
                    className={styles.input}
                    value={form.tempMax}
                    aria-label={t('topology.tempMax')}
                    onChange={(e) => setForm({ ...form, tempMax: Number(e.target.value) })}
                  />
                </label>
              </div>
            </section>

            <section className={styles.card}>
              <div className={styles.locHead}>
                <h3 className={styles.cardTitle}>
                  {t('topology.locations')} ({room.data.shownCount} {t('topology.of')}{' '}
                  {room.data.totalCount} {t('topology.shown')})
                </h3>
                <button
                  type="button"
                  className={styles.addLoc}
                  onClick={() => {
                    setAddForm({ address: '', capacity: 0, loadLimit: 0 });
                    setAddOpen(true);
                  }}
                >
                  <Plus size={14} aria-hidden /> {t('topology.addLoc.action')}
                </button>
              </div>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th>{t('topology.address')}</th>
                    <th className={styles.num}>{t('topology.capacity')}</th>
                    <th className={styles.num}>{t('topology.loadLimit')}</th>
                    <th className={styles.num}>{t('topology.occupied')}</th>
                    <th />
                  </tr>
                </thead>
                <tbody>
                  {room.data.locations.map((loc) => (
                    <tr key={loc.id}>
                      <td>{loc.address}</td>
                      <td className={styles.num}>{loc.capacity}</td>
                      <td className={styles.num}>{loc.loadLimit.toLocaleString()}</td>
                      <td className={styles.num}>{loc.occupied}</td>
                      <td>
                        <button
                          type="button"
                          className={styles.editLink}
                          onClick={() => openEdit(loc)}
                        >
                          {t('topology.edit')}
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              <div className={styles.warn}>
                <Thermometer size={14} aria-hidden /> {t('topology.deleteWarn')}
              </div>
            </section>

            <Modal
              open={!!editLoc}
              title={t('topology.editLoc.title')}
              onClose={() => setEditLoc(null)}
            >
              {editLoc ? (
                <>
                  <p className={styles.dialogHint}>{editLoc.address}</p>
                  <label className={styles.field}>
                    <span className={styles.fieldLabel}>{t('topology.capacity')}</span>
                    <input
                      type="number"
                      className={styles.input}
                      value={editForm.capacity}
                      aria-label={t('topology.capacity')}
                      onChange={(e) =>
                        setEditForm((f) => ({
                          ...f,
                          capacity: Number(e.target.value),
                        }))
                      }
                    />
                  </label>
                  <label className={styles.field}>
                    <span className={styles.fieldLabel}>{t('topology.loadLimit')}</span>
                    <input
                      type="number"
                      className={styles.input}
                      value={editForm.loadLimit}
                      aria-label={t('topology.loadLimit')}
                      onChange={(e) =>
                        setEditForm((f) => ({
                          ...f,
                          loadLimit: Number(e.target.value),
                        }))
                      }
                    />
                  </label>
                  <div className={styles.dialogActions}>
                    <button type="button" className={styles.ghost} onClick={() => setEditLoc(null)}>
                      {t('topology.cancel')}
                    </button>
                    <button
                      type="button"
                      className={styles.primary}
                      disabled={saveLocation.isPending}
                      onClick={submitEdit}
                    >
                      {t('topology.editLoc.submit')}
                    </button>
                  </div>
                </>
              ) : null}
            </Modal>

            <Modal
              open={addOpen}
              title={t('topology.addLoc.title')}
              onClose={() => setAddOpen(false)}
            >
              <label className={styles.field}>
                <span className={styles.fieldLabel}>{t('topology.address')}</span>
                <input
                  className={styles.input}
                  value={addForm.address}
                  aria-label={t('topology.address')}
                  onChange={(e) => setAddForm((f) => ({ ...f, address: e.target.value }))}
                />
              </label>
              <label className={styles.field}>
                <span className={styles.fieldLabel}>{t('topology.capacity')}</span>
                <input
                  type="number"
                  className={styles.input}
                  value={addForm.capacity}
                  aria-label={t('topology.capacity')}
                  onChange={(e) =>
                    setAddForm((f) => ({
                      ...f,
                      capacity: Number(e.target.value),
                    }))
                  }
                />
              </label>
              <label className={styles.field}>
                <span className={styles.fieldLabel}>{t('topology.loadLimit')}</span>
                <input
                  type="number"
                  className={styles.input}
                  value={addForm.loadLimit}
                  aria-label={t('topology.loadLimit')}
                  onChange={(e) =>
                    setAddForm((f) => ({
                      ...f,
                      loadLimit: Number(e.target.value),
                    }))
                  }
                />
              </label>
              <div className={styles.dialogActions}>
                <button type="button" className={styles.ghost} onClick={() => setAddOpen(false)}>
                  {t('topology.cancel')}
                </button>
                <button
                  type="button"
                  className={styles.primary}
                  disabled={!addForm.address.trim() || addLocation.isPending}
                  onClick={submitAdd}
                >
                  {t('topology.addLoc.submit')}
                </button>
              </div>
            </Modal>
          </>
        )}
      </div>
    </div>
  );
}
