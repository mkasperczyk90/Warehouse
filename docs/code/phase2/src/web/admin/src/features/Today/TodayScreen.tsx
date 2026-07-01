import { useTranslation } from 'react-i18next';
import { useNavigate } from '@tanstack/react-router';

import { StatusBadge } from '@/shared/ui';
import { useWorklist, type QueueKey, type WorklistItem } from './today.model';
import styles from './TodayScreen.module.css';

const CARD_KEYS = ['qc', 'expiring', 'partial', 'inbound', 'stocktake'] as const;
type CardKey = (typeof CARD_KEYS)[number];

const CARD_TONE: Record<CardKey, string> = {
  qc: 'qc',
  expiring: 'exp',
  partial: 'part',
  inbound: 'inb',
  stocktake: 'stk',
};

/** UC-cross-cutting landing — actionable queues, each a link to the screen that clears it. */
export function TodayScreen() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const wl = useWorklist();

  const goCard = (k: CardKey) => {
    switch (k) {
      case 'qc':
        navigate({ to: '/quality' });
        break;
      case 'expiring':
        navigate({ to: '/stock' });
        break;
      case 'partial':
        navigate({ to: '/outbound' });
        break;
      case 'inbound':
        navigate({ to: '/inbound' });
        break;
      case 'stocktake':
        navigate({ to: '/stocktake' });
        break;
    }
  };

  const goQueue = (key: QueueKey) => {
    switch (key) {
      case 'qc':
        navigate({ to: '/quality' });
        break;
      case 'partial':
        navigate({ to: '/outbound' });
        break;
      case 'inbound':
        navigate({ to: '/inbound' });
        break;
      case 'expiring':
        navigate({ to: '/stock' });
        break;
    }
  };

  const goItem = (key: QueueKey, item: WorklistItem) => {
    // Expiring items are stock items — deep-link to the drill-down; others to the section.
    if (key === 'expiring') navigate({ to: '/stock/$id', params: { id: item.id } });
    else goQueue(key);
  };

  if (wl.isLoading) return <p className={styles.state}>{t('state.loading')}</p>;
  if (wl.isError || !wl.data) return <p className={styles.state}>{t('state.error')}</p>;

  const { counts, queues } = wl.data;

  return (
    <>
      <div className={styles.head}>
        <h2 className={styles.title}>{t('today.title')}</h2>
        <div className={styles.sub}>{t('today.sub')}</div>
      </div>

      <div className={styles.cards}>
        {CARD_KEYS.map((k) => (
          <button key={k} type="button" className={`${styles.card} ${styles[CARD_TONE[k]]}`} onClick={() => goCard(k)}>
            <div className={styles.cardV}>{counts[k]}</div>
            <div className={styles.cardL}>{t(`today.card.${k}`)}</div>
            <div className={styles.cardX}>{t(`today.card.${k}Ctx`)}</div>
          </button>
        ))}
      </div>

      <div className={styles.grid2}>
        {queues.map((q) => (
          <div key={q.key} className={styles.panel}>
            <div className={styles.ph}>
              <b>
                {t(`today.queue.${q.key}`)} <span className={styles.n}>· {q.shownNote ?? q.count}</span>
              </b>
              <button type="button" className={styles.viewAll} onClick={() => goQueue(q.key)}>
                {t('today.viewAll')}
              </button>
            </div>
            {q.items.map((item) => (
              <button key={item.id} type="button" className={styles.row} onClick={() => goItem(q.key, item)}>
                <span className={styles.nm}>
                  {item.label}
                  <span className={styles.sub2}>{item.sublabel}</span>
                </span>
                <span className={styles.meta}>
                  {item.badge ? <StatusBadge variant={item.badge.variant} label={item.badge.label} /> : null}
                  {item.meta ? <span className={styles.metaText}>{item.meta}</span> : null}
                </span>
              </button>
            ))}
          </div>
        ))}
      </div>
    </>
  );
}
