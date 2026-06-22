import { Search } from 'lucide-react';

import styles from './FilterBar.module.css';

export interface FilterPill {
  key: string;
  label: string;
}

/** Search + quick-filter pills, sitting above a DataTable inside a panel. */
export function FilterBar({
  searchPlaceholder,
  search,
  onSearch,
  pills,
  activePill,
  onPill,
}: {
  searchPlaceholder: string;
  search: string;
  onSearch: (value: string) => void;
  pills: FilterPill[];
  activePill: string;
  onPill: (key: string) => void;
}) {
  return (
    <div className={styles.filters}>
      <div className={styles.search}>
        <Search size={16} aria-hidden />
        <input
          value={search}
          onChange={(e) => onSearch(e.target.value)}
          placeholder={searchPlaceholder}
          aria-label={searchPlaceholder}
        />
      </div>
      {pills.map((pill) => (
        <button
          key={pill.key}
          type="button"
          className={`${styles.pill} ${activePill === pill.key ? styles.on : ''}`}
          onClick={() => onPill(pill.key)}
        >
          {pill.label}
        </button>
      ))}
    </div>
  );
}
