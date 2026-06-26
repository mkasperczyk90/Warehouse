import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from '@tanstack/react-router';
import { useQueryClient } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';

import { Modal, StatusBadge, type StatusVariant } from '@/shared/ui';
import {
  STORAGE_MODES,
  useChangeStorage,
  useProduct,
  useRenameProduct,
  type StorageMode,
} from './product.model';
import styles from './ProductDetailScreen.module.css';

const STORAGE_VARIANT: Record<StorageMode, StatusVariant> = {
  Ambient: 'available',
  ColdChain: 'reserved',
  Hazardous: 'blocked',
};

export function ProductDetailScreen({ sku }: { sku: string }) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const product = useProduct(sku);

  const rename = useRenameProduct(sku);
  const changeStorage = useChangeStorage(sku);

  const [renameOpen, setRenameOpen] = useState(false);
  const [storageOpen, setStorageOpen] = useState(false);
  const [name, setName] = useState('');
  const [mode, setMode] = useState<StorageMode>('Ambient');
  const [min, setMin] = useState('');
  const [max, setMax] = useState('');

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['products', 'detail', sku] });
    queryClient.invalidateQueries({ queryKey: ['products', 'list'] });
  };

  if (product.isLoading) return <p className={styles.state}>{t('state.loading')}</p>;
  if (product.isError || !product.data) return <p className={styles.state}>{t('state.error')}</p>;

  const p = product.data;

  const openRename = () => {
    setName(p.name);
    setRenameOpen(true);
  };
  const openStorage = () => {
    setMode(p.storage.mode);
    setMin(p.storage.minCelsius?.toString() ?? '');
    setMax(p.storage.maxCelsius?.toString() ?? '');
    setStorageOpen(true);
  };

  const submitRename = () =>
    rename.mutate(name.trim(), {
      onSuccess: () => {
        invalidate();
        setRenameOpen(false);
      },
    });

  const submitStorage = () =>
    changeStorage.mutate(
      {
        storage: mode,
        minCelsius: min === '' ? null : Number(min),
        maxCelsius: max === '' ? null : Number(max),
      },
      {
        onSuccess: () => {
          invalidate();
          setStorageOpen(false);
        },
      },
    );

  return (
    <>
      <button type="button" className={styles.back} onClick={() => navigate({ to: '/products' })}>
        <ArrowLeft size={14} aria-hidden /> {t('products.catalogTitle')}
      </button>

      <div className={styles.head}>
        <div>
          <h2 className={styles.title}>{p.name}</h2>
          <div className={styles.sku}>{p.sku}</div>
        </div>
        <div className={styles.actions}>
          <button type="button" className={styles.ghost} onClick={openRename}>
            {t('products.rename')}
          </button>
          <button type="button" className={styles.ghost} onClick={openStorage}>
            {t('products.changeStorage')}
          </button>
        </div>
      </div>

      <dl className={styles.grid}>
        <div>
          <dt>{t('products.f.ean')}</dt>
          <dd>{p.ean ?? t('products.none')}</dd>
        </div>
        <div>
          <dt>{t('products.category')}</dt>
          <dd>{t(`products.categoryOpt.${p.category}`)}</dd>
        </div>
        <div>
          <dt>{t('products.unit')}</dt>
          <dd>{t(`products.unitOpt.${p.baseUnit}`)}</dd>
        </div>
        <div>
          <dt>{t('products.storageCol')}</dt>
          <dd>
            <StatusBadge
              variant={STORAGE_VARIANT[p.storage.mode]}
              label={t(`products.storageOpt.${p.storage.mode}`)}
            />
            {p.storage.minCelsius != null && p.storage.maxCelsius != null ? (
              <span className={styles.temp}>
                {' '}
                {p.storage.minCelsius}…{p.storage.maxCelsius} °C
              </span>
            ) : null}
          </dd>
        </div>
        <div>
          <dt>{t('products.f.dimensions')}</dt>
          <dd>
            {p.dimensions.lengthCm}×{p.dimensions.widthCm}×{p.dimensions.heightCm} cm
          </dd>
        </div>
        <div>
          <dt>{t('products.f.weight')}</dt>
          <dd>{p.unitWeightKg} kg</dd>
        </div>
        <div>
          <dt>{t('col.tracking')}</dt>
          <dd>
            {p.isBatchTracked ? (
              <span className={styles.chip}>{t('products.batchChip')}</span>
            ) : null}
            {p.hasExpiryDate ? <span className={styles.chip}>{t('products.fefoChip')}</span> : null}
            {!p.isBatchTracked && !p.hasExpiryDate ? <span className={styles.muted}>—</span> : null}
          </dd>
        </div>
      </dl>

      <Modal
        open={renameOpen}
        title={t('products.renameTitle')}
        onClose={() => setRenameOpen(false)}
      >
        <div className={styles.form}>
          <label className={styles.field}>
            <span>{t('products.newName')}</span>
            <input value={name} onChange={(e) => setName(e.target.value)} />
          </label>
          <div className={styles.modalActions}>
            <button type="button" className={styles.ghost} onClick={() => setRenameOpen(false)}>
              {t('products.cancel')}
            </button>
            <button
              type="button"
              className={styles.primary}
              disabled={rename.isPending || name.trim() === ''}
              onClick={submitRename}
            >
              {t('products.save')}
            </button>
          </div>
        </div>
      </Modal>

      <Modal
        open={storageOpen}
        title={t('products.changeStorageTitle')}
        onClose={() => setStorageOpen(false)}
      >
        <div className={styles.form}>
          <label className={styles.field}>
            <span>{t('products.f.storage')}</span>
            <select value={mode} onChange={(e) => setMode(e.target.value as StorageMode)}>
              {STORAGE_MODES.map((m) => (
                <option key={m} value={m}>
                  {t(`products.storageOpt.${m}`)}
                </option>
              ))}
            </select>
          </label>
          {mode !== 'Ambient' ? (
            <div className={styles.row2}>
              <label className={styles.field}>
                <span>{t('products.f.tempMin')}</span>
                <input value={min} onChange={(e) => setMin(e.target.value)} />
              </label>
              <label className={styles.field}>
                <span>{t('products.f.tempMax')}</span>
                <input value={max} onChange={(e) => setMax(e.target.value)} />
              </label>
            </div>
          ) : null}
          <p className={styles.note}>{t('products.storageNote')}</p>
          <div className={styles.modalActions}>
            <button type="button" className={styles.ghost} onClick={() => setStorageOpen(false)}>
              {t('products.cancel')}
            </button>
            <button
              type="button"
              className={styles.primary}
              disabled={changeStorage.isPending}
              onClick={submitStorage}
            >
              {t('products.apply')}
            </button>
          </div>
        </div>
      </Modal>
    </>
  );
}

/** Param-route wrapper: reads `$sku` and renders the detail screen. */
export function ProductDetailRoute() {
  const params = useParams({ strict: false });
  return <ProductDetailScreen sku={params.sku as string} />;
}
