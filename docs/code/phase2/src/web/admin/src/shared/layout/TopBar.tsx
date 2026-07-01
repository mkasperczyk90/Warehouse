import { ChevronDown } from 'lucide-react';
import { useRouterState } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';

import { GlobalSearch } from '@/features/Search';
import { ROUTE_META } from '@/navigation/routes';
import styles from './TopBar.module.css';

export function TopBar() {
  const { t, i18n } = useTranslation();
  const pathname = useRouterState({ select: (s) => s.location.pathname });
  const meta = ROUTE_META[pathname];

  const toggleLang = () => void i18n.changeLanguage(i18n.language === 'en' ? 'pl' : 'en');

  return (
    <div className={styles.top}>
      <div className={styles.crumb}>
        {meta ? (
          meta.groupKey ? (
            <>
              {t(meta.groupKey)} / <b>{t(meta.titleKey)}</b>
            </>
          ) : (
            <b>{t(meta.titleKey)}</b>
          )
        ) : null}
      </div>

      <GlobalSearch />

      <div className={styles.who}>
        <button type="button" className={styles.lang} onClick={toggleLang}>
          {i18n.language.toUpperCase()}
        </button>
        <span className={styles.warehouse}>
          {t('app.warehouse')} <ChevronDown size={14} aria-hidden />
        </span>
        <span className={styles.avatar}>KM</span>
        <span>{t('app.user')}</span>
      </div>
    </div>
  );
}
