import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from '@tanstack/react-router';
import {
  ArrowDownToLine,
  ArrowUpRight,
  Boxes,
  Grid3x3,
  Search,
  Tags,
  Truck,
  type LucideIcon,
} from 'lucide-react';

import { useGlobalSearch, type SearchResult, type SearchType } from './search.model';
import styles from './GlobalSearch.module.css';

const ICON: Record<SearchType, LucideIcon> = {
  product: Tags,
  stock: Boxes,
  asn: ArrowDownToLine,
  order: ArrowUpRight,
  shipment: Truck,
  location: Grid3x3,
};

/** The command-bar in the top bar — search anything, jump to it. */
export function GlobalSearch() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const search = useGlobalSearch(query);

  const go = (r: SearchResult) => {
    setQuery('');
    setOpen(false);
    switch (r.type) {
      case 'product':
        navigate({ to: '/products/$sku', params: { sku: r.refId } });
        break;
      case 'stock':
        navigate({ to: '/stock/$id', params: { id: r.refId } });
        break;
      case 'asn':
        navigate({ to: '/inbound' });
        break;
      case 'order':
        navigate({ to: '/outbound' });
        break;
      case 'shipment':
        navigate({ to: '/dispatch' });
        break;
      case 'location':
        navigate({ to: '/topology' });
        break;
    }
  };

  const showPopover = open && query.trim().length >= 2;
  const results = search.data ?? [];

  return (
    <div className={styles.wrap}>
      <div className={styles.box}>
        <Search size={16} aria-hidden />
        <input
          className={styles.input}
          value={query}
          placeholder={t('search.placeholder')}
          aria-label={t('search.placeholder')}
          onChange={(e) => setQuery(e.target.value)}
          onFocus={() => setOpen(true)}
          onBlur={() => setTimeout(() => setOpen(false), 120)}
          onKeyDown={(e) => {
            if (e.key === 'Escape') {
              setQuery('');
              (e.target as HTMLInputElement).blur();
            }
          }}
        />
      </div>

      {showPopover ? (
        <div className={styles.popover}>
          {search.isLoading ? (
            <div className={styles.empty}>{t('state.loading')}</div>
          ) : results.length === 0 ? (
            <div className={styles.empty}>{t('search.noResults')}</div>
          ) : (
            results.map((r) => {
              const Icon = ICON[r.type];
              return (
                <button
                  key={`${r.type}-${r.refId}`}
                  type="button"
                  className={styles.item}
                  // mousedown fires before the input's blur, so the click lands
                  onMouseDown={() => go(r)}
                >
                  <Icon size={16} className={styles.itemIcon} aria-hidden />
                  <span className={styles.itemMain}>
                    <span className={styles.itemLabel}>{r.label}</span>
                    <span className={styles.itemSub}>{r.sublabel}</span>
                  </span>
                  <span className={styles.itemType}>{t(`search.types.${r.type}`)}</span>
                </button>
              );
            })
          )}
        </div>
      ) : null}
    </div>
  );
}
