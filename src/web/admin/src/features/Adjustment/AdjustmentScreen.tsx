import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { Lock } from 'lucide-react';
import { useParams } from '@tanstack/react-router';

import { Modal, StatusBadge } from '@/shared/ui';
import {
  ADJUSTMENT_REASONS,
  adjustmentSchema,
  useAdjustmentDraft,
  usePostAdjustment,
  type AdjustmentForm,
} from './adjustment.model';
import styles from './AdjustmentScreen.module.css';

/** Route component for `/adjustment/$itemId` — adjusts a specific stock item. */
export function AdjustmentRoute() {
  const params = useParams({ strict: false });
  return <AdjustmentScreen itemId={params.itemId} />;
}

export function AdjustmentScreen({ itemId }: { itemId?: string } = {}) {
  const { t } = useTranslation();
  const draft = useAdjustmentDraft(itemId);
  const mutation = usePostAdjustment();

  const form = useForm<AdjustmentForm>({
    resolver: zodResolver(adjustmentSchema),
    defaultValues: { newQuantity: 0, note: '' },
  });
  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = form;

  // Seed the form once the draft (system on-hand) arrives.
  useEffect(() => {
    if (draft.data) reset({ newQuantity: draft.data.systemOnHand, note: '' });
  }, [draft.data, reset]);

  // Posting to the immutable ledger is irreversible — gate it behind a confirm.
  const [confirm, setConfirm] = useState<AdjustmentForm | null>(null);

  if (draft.isLoading) return <p className={styles.state}>{t('state.loading')}</p>;
  if (draft.isError || !draft.data) return <p className={styles.state}>{t('state.error')}</p>;

  const item = draft.data;
  const newQuantity = watch('newQuantity');
  const delta = (Number.isFinite(newQuantity) ? newQuantity : item.systemOnHand) - item.systemOnHand;

  // Validation runs first (handleSubmit); only a valid form opens the confirm.
  const onSubmit = (values: AdjustmentForm) => setConfirm(values);

  const confirmDelta = confirm ? confirm.newQuantity - item.systemOnHand : 0;
  const doPost = () => {
    if (!confirm) return;
    mutation.mutate({ ...confirm, itemId: item.itemId });
    setConfirm(null);
  };

  return (
    <>
    <form className={styles.content} onSubmit={handleSubmit(onSubmit)}>
      <div className={styles.head}>
        <h2 className={styles.title}>{t('adjustment.title')}</h2>
        <div className={styles.sub}>{t('adjustment.subtitle')}</div>
      </div>

      <section className={styles.card}>
        <h3 className={styles.cardTitle}>{t('adjustment.itemTitle')}</h3>
        <div className={styles.item}>
          <div>
            <b>
              {item.product} · {item.batch}
            </b>
            <div className={styles.itemSku}>
              SKU {item.sku} · {item.location} · {item.room}
            </div>
          </div>
          <span className={styles.itemBadge}>
            <StatusBadge variant={item.status} label={item.statusLabel} />
          </span>
        </div>
      </section>

      <section className={styles.card}>
        <h3 className={styles.cardTitle}>{t('adjustment.adjustmentTitle')}</h3>
        <div className={styles.grid}>
          <Field label={t('adjustment.systemOnHand')}>
            <div className={`${styles.input} ${styles.ro}`}>
              {item.systemOnHand.toLocaleString()} {item.unit}
            </div>
          </Field>

          <Field
            label={t('adjustment.newQuantity')}
            error={errors.newQuantity?.message}
          >
            <input
              type="number"
              className={styles.input}
              {...register('newQuantity', { valueAsNumber: true })}
            />
          </Field>

          <Field label={t('adjustment.delta')}>
            <div className={`${styles.delta} ${delta > 0 ? styles.pos : delta < 0 ? styles.neg : ''}`}>
              {delta > 0 ? '+' : ''}
              {delta}
            </div>
          </Field>

          <Field
            label={`${t('adjustment.reason')} *`}
            error={errors.reason ? t('adjustment.reasonRequired') : undefined}
          >
            <select className={styles.input} defaultValue="" {...register('reason')}>
              <option value="" disabled>
                {t('adjustment.selectReason')}
              </option>
              {ADJUSTMENT_REASONS.map((r) => (
                <option key={r} value={r}>
                  {t(`adjustment.reasonOpt.${r}`)}
                </option>
              ))}
            </select>
          </Field>

          <Field label={t('adjustment.note')} span={2}>
            <input className={styles.input} {...register('note')} />
          </Field>
        </div>

        <div className={styles.result}>
          <span className={styles.resultLabel}>{t('adjustment.afterPosting')}</span>
          <span className={styles.resultValue}>
            {(Number.isFinite(newQuantity) ? newQuantity : item.systemOnHand).toLocaleString()} {item.unit}{' '}
            {t('adjustment.onHand')}
          </span>
          <span className={styles.resultLabel}>{t('adjustment.ledgerMovement')}</span>
          <span className={styles.resultValue}>
            {delta > 0 ? '+' : ''}
            {delta}
          </span>
        </div>

        <div className={styles.audit}>
          <Lock size={14} aria-hidden /> {t('adjustment.audit')}
        </div>
      </section>

      {mutation.isSuccess ? (
        <div className={styles.success}>{t('adjustment.posted')}</div>
      ) : mutation.isError ? (
        <div className={styles.error}>{t('state.error')}</div>
      ) : null}

      <div className={styles.actions}>
        <button type="button" className={styles.ghost} onClick={() => reset()}>
          {t('adjustment.cancel')}
        </button>
        <button type="submit" className={styles.primary} disabled={mutation.isPending}>
          {t('adjustment.post')}
        </button>
      </div>
    </form>

      <Modal
        open={!!confirm}
        title={t('adjustment.confirm.title')}
        onClose={() => setConfirm(null)}
      >
        {confirm ? (
          <>
            <p className={styles.confirmItem}>
              {item.product} · {item.batch}
            </p>
            <div className={styles.confirmRow}>
              <span>{t('adjustment.delta')}</span>
              <b className={confirmDelta < 0 ? styles.neg : styles.pos}>
                {confirmDelta > 0 ? '+' : ''}
                {confirmDelta}
              </b>
            </div>
            <div className={styles.confirmRow}>
              <span>{t('adjustment.reason')}</span>
              <b>{t(`adjustment.reasonOpt.${confirm.reason}`)}</b>
            </div>
            <div className={styles.confirmRow}>
              <span>{t('adjustment.afterPosting')}</span>
              <b>
                {confirm.newQuantity.toLocaleString()} {item.unit} {t('adjustment.onHand')}
              </b>
            </div>
            <div className={styles.confirmWarn}>{t('adjustment.confirm.warn')}</div>
            <div className={styles.confirmActions}>
              <button type="button" className={styles.ghost} onClick={() => setConfirm(null)}>
                {t('adjustment.cancel')}
              </button>
              <button
                type="button"
                className={styles.primary}
                onClick={doPost}
                disabled={mutation.isPending}
              >
                {t('adjustment.confirm.post')}
              </button>
            </div>
          </>
        ) : null}
      </Modal>
    </>
  );
}

function Field({
  label,
  children,
  error,
  span = 1,
}: {
  label: string;
  children: React.ReactNode;
  error?: string;
  span?: number;
}) {
  return (
    <label className={styles.field} style={span > 1 ? { gridColumn: `span ${span}` } : undefined}>
      <span className={styles.fieldLabel}>{label}</span>
      {children}
      {error ? <div className={styles.fieldError}>{error}</div> : null}
    </label>
  );
}
