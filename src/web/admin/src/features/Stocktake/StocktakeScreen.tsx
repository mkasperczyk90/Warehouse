import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useParams } from '@tanstack/react-router';

import {
  STOCKTAKE_REASONS,
  useApproveStocktake,
  useRecountStocktake,
  useStocktake,
  type StocktakeReason,
} from './stocktake.model';
import styles from './StocktakeScreen.module.css';

/** Route component for `/stocktake/$id`. */
export function StocktakeReviewRoute() {
  const params = useParams({ strict: false });
  return <StocktakeScreen id={params.id} />;
}

export function StocktakeScreen({ id }: { id?: string }) {
  const { t } = useTranslation();
  const stocktake = useStocktake(id);
  const approve = useApproveStocktake(stocktake.data?.summary.id);
  const recount = useRecountStocktake(stocktake.data?.summary.id);
  const [recountBanner, setRecountBanner] = useState<string | null>(null);

  // Row selection + per-row reason, seeded from the operator's pre-fills.
  const [selected, setSelected] = useState<Record<string, boolean>>({});
  const [reasons, setReasons] = useState<Record<string, StocktakeReason | ''>>({});
  const [seeded, setSeeded] = useState(false);

  if (stocktake.data && !seeded) {
    const sel: Record<string, boolean> = {};
    const rsn: Record<string, StocktakeReason | ''> = {};
    for (const d of stocktake.data.diffs) {
      sel[d.id] = d.defaultReason !== undefined;
      rsn[d.id] = d.defaultReason ?? '';
    }
    setSelected(sel);
    setReasons(rsn);
    setSeeded(true);
  }

  const diffs = useMemo(
    () => stocktake.data?.diffs ?? [],
    [stocktake.data?.diffs],
  );
  const selectedIds = useMemo(
    () => diffs.filter((d) => selected[d.id]).map((d) => d.id),
    [diffs, selected],
  );

  // Reason required to post: every selected row must carry one (UC-07 rule).
  const canApprove = selectedIds.length > 0 && selectedIds.every((id) => reasons[id]);

  const onApprove = () => {
    approve.mutate(selectedIds.map((id) => ({ id, reason: reasons[id] as StocktakeReason })));
  };

  const onRecount = () => {
    if (selectedIds.length === 0) return;
    recount.mutate(selectedIds, {
      onSuccess: () => setRecountBanner(t('stocktake.recountDone', { n: selectedIds.length })),
    });
  };

  if (stocktake.isLoading) return <p className={styles.state}>{t('state.loading')}</p>;
  if (stocktake.isError || !stocktake.data)
    return <p className={styles.state}>{t('state.error')}</p>;

  const { summary } = stocktake.data;
  const isReview = summary.state === 'review';

  return (
    <>
      <div className={styles.head}>
        <div>
          <h2 className={styles.title}>{summary.title}</h2>
          <div className={styles.sub}>{summary.sub}</div>
        </div>
        {isReview ? (
          <div className={styles.actions}>
            <button
              type="button"
              className={styles.ghost}
              disabled={selectedIds.length === 0 || recount.isPending}
              onClick={onRecount}
            >
              {t('stocktake.recount')}
            </button>
            <button
              type="button"
              className={styles.primary}
              disabled={!canApprove || approve.isPending || approve.isSuccess}
              onClick={onApprove}
            >
              {approve.isSuccess ? t('stocktake.approved') : t('stocktake.approve')}
            </button>
          </div>
        ) : null}
      </div>

      {recountBanner ? <div className={styles.recountBanner}>{recountBanner}</div> : null}

      <div className={styles.summary}>
        <SummaryCard
          label={t('stocktake.card.locationsCounted')}
          value={summary.locationsCounted}
        />
        <SummaryCard label={t('stocktake.card.matches')} value={summary.matches} />
        <SummaryCard
          label={t('stocktake.card.discrepancies')}
          value={summary.discrepancies}
          tone="neg"
        />
        <SummaryCard
          label={t('stocktake.card.netVariance')}
          value={summary.netVariance}
          tone={summary.netVariance < 0 ? 'neg' : 'pos'}
          signed
        />
      </div>

      {isReview ? (
        <div className={styles.panel}>
          <div className={styles.panelHead}>
            <span>{t('stocktake.panelTitle')}</span>
            <small>{t('stocktake.panelHint')}</small>
          </div>
          <table className={styles.table}>
            <thead>
              <tr>
                <th />
                <th>{t('col.location')}</th>
                <th>{t('stocktake.col.productBatch')}</th>
                <th className={styles.num}>{t('stocktake.col.system')}</th>
                <th className={styles.num}>{t('stocktake.col.counted')}</th>
                <th className={styles.num}>{t('stocktake.col.delta')}</th>
                <th>{t('stocktake.col.reason')}</th>
              </tr>
            </thead>
            <tbody>
              {diffs.map((d) => (
                <tr key={d.id}>
                  <td>
                    <input
                      type="checkbox"
                      className={styles.chk}
                      checked={!!selected[d.id]}
                      aria-label={`select ${d.location}`}
                      onChange={(e) =>
                        setSelected((s) => ({
                          ...s,
                          [d.id]: e.target.checked,
                        }))
                      }
                    />
                  </td>
                  <td>{d.location}</td>
                  <td>
                    {d.product} · {d.batch}
                  </td>
                  <td className={styles.num}>{d.system.toLocaleString()}</td>
                  <td className={styles.num}>{d.counted.toLocaleString()}</td>
                  <td className={`${styles.num} ${d.delta < 0 ? styles.neg : styles.pos}`}>
                    {d.delta > 0 ? '+' : ''}
                    {d.delta}
                  </td>
                  <td>
                    <select
                      className={styles.reason}
                      value={reasons[d.id] ?? ''}
                      aria-label={`reason ${d.location}`}
                      onChange={(e) =>
                        setReasons((r) => ({
                          ...r,
                          [d.id]: e.target.value as StocktakeReason | '',
                        }))
                      }
                    >
                      <option value="">{t('stocktake.selectReason')}</option>
                      {STOCKTAKE_REASONS.map((r) => (
                        <option key={r} value={r}>
                          {t(`stocktake.reasonOpt.${r}`)}
                        </option>
                      ))}
                    </select>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : (
        <div className={styles.banner}>
          {summary.state === 'counting'
            ? t('stocktake.countingBanner', {
                counted: summary.locationsCounted,
                total: summary.totalLocations,
              })
            : t(`stocktake.state.${summary.state}`)}
        </div>
      )}
    </>
  );
}

function SummaryCard({
  label,
  value,
  tone,
  signed = false,
}: {
  label: string;
  value: number;
  tone?: 'pos' | 'neg';
  signed?: boolean;
}) {
  return (
    <div className={styles.card}>
      <div className={styles.cardLabel}>{label}</div>
      <div
        className={`${styles.cardValue} ${tone === 'neg' ? styles.neg : tone === 'pos' ? styles.pos : ''}`}
      >
        {signed && value > 0 ? '+' : ''}
        {value.toLocaleString()}
      </div>
    </div>
  );
}
