import { useEffect } from 'react';
import { useForm, type UseFormRegisterReturn } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslation } from 'react-i18next';
import { TriangleAlert } from 'lucide-react';
import { useParams } from '@tanstack/react-router';

import { StatusBadge } from '@/shared/ui';
import {
  PRODUCT_CATEGORIES,
  PRODUCT_UNITS,
  productSchema,
  useProduct,
  useSaveProduct,
  type ProductForm,
} from './product.model';
import styles from './ProductEditScreen.module.css';

const NEW_DEFAULTS: ProductForm = {
  sku: '',
  name: '',
  ean: '',
  category: 'dairy',
  unit: 'ea',
  length: 0,
  width: 0,
  height: 0,
  weight: 0,
  packConversion: '',
  tempMin: 0,
  tempMax: 0,
  hazardous: false,
  batchTracked: false,
  expiryTracked: false,
};

/** Route component for `/products/$sku` — reads the SKU and edits that product. */
export function ProductEditRoute() {
  const params = useParams({ strict: false });
  return <ProductEditScreen sku={params.sku} />;
}

/** Edit an existing product (sku given) or create a new one (sku undefined). */
export function ProductEditScreen({ sku }: { sku?: string }) {
  const { t } = useTranslation();
  const isNew = !sku;
  const product = useProduct(sku);
  const mutation = useSaveProduct();

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<ProductForm>({ resolver: zodResolver(productSchema), defaultValues: NEW_DEFAULTS });

  useEffect(() => {
    if (isNew) {
      reset(NEW_DEFAULTS);
    } else if (product.data) {
      const { lastEdited: _lastEdited, ...values } = product.data;
      reset(values);
    }
  }, [isNew, product.data, reset]);

  if (!isNew && product.isLoading) return <p className={styles.state}>{t('state.loading')}</p>;
  if (!isNew && (product.isError || !product.data))
    return <p className={styles.state}>{t('state.error')}</p>;

  const tempMax = watch('tempMax');
  const coldOnly = Number.isFinite(tempMax) && tempMax <= 8;

  const onSubmit = (values: ProductForm) => mutation.mutate(values);

  return (
    <form className={styles.content} onSubmit={handleSubmit(onSubmit)}>
      <div className={styles.head}>
        <div>
          <h2 className={styles.title}>{isNew ? t('products.newTitle') : product.data?.name}</h2>
          <div className={styles.sub}>
            {isNew
              ? t('products.newSub')
              : `ProductType · SKU ${sku} · ${t('products.lastEdited')} ${product.data?.lastEdited}`}
          </div>
        </div>
        <div className={styles.actions}>
          <button type="button" className={styles.ghost} onClick={() => reset()}>
            {t('products.cancel')}
          </button>
          <button type="submit" className={styles.primary} disabled={mutation.isPending}>
            {isNew ? t('products.create') : t('products.save')}
          </button>
        </div>
      </div>

      {mutation.isSuccess ? <div className={styles.success}>{t('products.saved')}</div> : null}

      <section className={styles.card}>
        <h3 className={styles.cardTitle}>{t('products.identity')}</h3>
        <div className={styles.grid}>
          <Field label={t('products.sku')} error={errors.sku?.message}>
            <input
              className={`${styles.input} ${isNew ? '' : styles.ro}`}
              disabled={!isNew}
              {...register('sku')}
            />
          </Field>
          <Field label={t('products.name')} span={2} error={errors.name?.message}>
            <input className={styles.input} {...register('name')} />
          </Field>
          <Field label={t('products.ean')} error={errors.ean?.message}>
            <input className={styles.input} {...register('ean')} />
          </Field>
          <Field label={t('products.category')}>
            <select className={styles.input} {...register('category')}>
              {PRODUCT_CATEGORIES.map((c) => (
                <option key={c} value={c}>
                  {t(`products.categoryOpt.${c}`)}
                </option>
              ))}
            </select>
          </Field>
          <Field label={t('products.unit')}>
            <select className={styles.input} {...register('unit')}>
              {PRODUCT_UNITS.map((u) => (
                <option key={u} value={u}>
                  {t(`products.unitOpt.${u}`)}
                </option>
              ))}
            </select>
          </Field>
        </div>
      </section>

      <section className={styles.card}>
        <h3 className={styles.cardTitle}>{t('products.dimensions')}</h3>
        <div className={styles.grid}>
          <Field label={t('products.length')}>
            <input type="number" className={styles.input} {...register('length', { valueAsNumber: true })} />
          </Field>
          <Field label={t('products.width')}>
            <input type="number" className={styles.input} {...register('width', { valueAsNumber: true })} />
          </Field>
          <Field label={t('products.height')}>
            <input type="number" className={styles.input} {...register('height', { valueAsNumber: true })} />
          </Field>
          <Field label={t('products.weight')}>
            <input type="number" className={styles.input} {...register('weight', { valueAsNumber: true })} />
          </Field>
          <Field label={t('products.packConversion')} span={2}>
            <input className={styles.input} {...register('packConversion')} />
          </Field>
        </div>
      </section>

      <section className={styles.card}>
        <h3 className={styles.cardTitle}>
          {t('products.storage')} <span className={styles.cardHint}>— {t('products.storageHint')}</span>
        </h3>
        <div className={styles.grid}>
          <Field label={t('products.tempRange')} error={errors.tempMax?.message}>
            <div className={styles.temp}>
              <input
                type="number"
                className={styles.tempInput}
                aria-label={t('products.tempMinAria')}
                {...register('tempMin', { valueAsNumber: true })}
              />
              <span>–</span>
              <input
                type="number"
                className={styles.tempInput}
                aria-label={t('products.tempMaxAria')}
                {...register('tempMax', { valueAsNumber: true })}
              />
              <span>°C</span>
              <StatusBadge
                variant="reserved"
                label={coldOnly ? t('products.coldOnly') : t('products.ambientOk')}
              />
            </div>
          </Field>
          <Toggle label={t('products.hazardous')} {...register('hazardous')} />
          <Toggle label={t('products.batchTracked')} {...register('batchTracked')} />
          <Toggle label={t('products.expiryTracked')} {...register('expiryTracked')} />
        </div>
        <div className={styles.note}>
          <TriangleAlert size={14} aria-hidden /> {t('products.storageNote')}
        </div>
      </section>
    </form>
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

// A switch-styled checkbox; forwards the RHF register props to the input.
const Toggle = ({ label, ...register }: { label: string } & UseFormRegisterReturn) => (
  <label className={styles.field}>
    <span className={styles.fieldLabel}>{label}</span>
    <span className={styles.toggle}>
      <input type="checkbox" className={styles.switch} aria-label={label} {...register} />
    </span>
  </label>
);
