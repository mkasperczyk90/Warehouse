import { useState } from 'react';
import { Check, ChevronDown, LogOut, User, Warehouse } from 'lucide-react';
import { Link, useRouterState } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';

import { GlobalSearch } from '@/features/Search';
import { warehouseLabel } from '@/features/Warehouses';
import { ROUTE_META, ROUTES } from '@/navigation/routes';
import { useAuth } from '@/shared/auth/AuthContext';
import { useActiveWarehouse } from '@/shared/warehouse/WarehouseContext';
import styles from './TopBar.module.css';

type OpenMenu = 'warehouse' | 'user' | null;

export function TopBar() {
  const { t, i18n } = useTranslation();
  const pathname = useRouterState({ select: (s) => s.location.pathname });
  const meta = ROUTE_META[pathname];

  const { user, logout } = useAuth();
  const { active, warehouses, warehouseId, setWarehouseId } = useActiveWarehouse();
  const [open, setOpen] = useState<OpenMenu>(null);

  const toggleLang = () => void i18n.changeLanguage(i18n.language === 'en' ? 'pl' : 'en');
  const initials = (user?.name ?? '')
    .split(/\s+/)
    .map((s) => s[0])
    .slice(0, 2)
    .join('')
    .toUpperCase();

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
        {/* --- Warehouse switcher ------------------------------------------ */}
        <div className={styles.menuWrap}>
          <button
            type="button"
            className={styles.warehouse}
            onClick={() => setOpen(open === 'warehouse' ? null : 'warehouse')}
            aria-haspopup="listbox"
            aria-expanded={open === 'warehouse'}
          >
            <Warehouse size={14} aria-hidden />
            {active ? warehouseLabel(active) : t('app.warehouse')}
            <ChevronDown size={14} aria-hidden />
          </button>
          {open === 'warehouse' ? (
            <ul className={styles.menu} role="listbox" aria-label={t('app.switchWarehouse')}>
              {warehouses.map((w) => (
                <li key={w.id}>
                  <button
                    type="button"
                    role="option"
                    aria-selected={w.id === warehouseId}
                    className={styles.menuItem}
                    onClick={() => {
                      setWarehouseId(w.id);
                      setOpen(null);
                    }}
                  >
                    <span>{warehouseLabel(w)}</span>
                    {w.id === warehouseId ? <Check size={14} aria-hidden /> : null}
                  </button>
                </li>
              ))}
            </ul>
          ) : null}
        </div>

        {/* --- User / profile menu ----------------------------------------- */}
        <div className={styles.menuWrap}>
          <button
            type="button"
            className={styles.userBtn}
            onClick={() => setOpen(open === 'user' ? null : 'user')}
            aria-haspopup="menu"
            aria-expanded={open === 'user'}
          >
            <span className={styles.avatar}>{initials || '—'}</span>
            <span>{user?.name ?? t('app.user')}</span>
          </button>
          {open === 'user' ? (
            <div className={styles.menu} role="menu">
              <div className={styles.menuHead}>
                <div className={styles.menuName}>{user?.name}</div>
                <div className={styles.menuRole}>
                  {user ? t(`profile.role.${user.role}`) : null}
                </div>
              </div>
              <Link
                to={ROUTES.profile}
                role="menuitem"
                className={styles.menuItem}
                onClick={() => setOpen(null)}
              >
                <User size={14} aria-hidden /> {t('app.profile')}
              </Link>
              <button
                type="button"
                role="menuitem"
                className={styles.menuItem}
                onClick={() => {
                  toggleLang();
                  setOpen(null);
                }}
              >
                <span className={styles.langTag}>{i18n.language.toUpperCase()}</span>
                {t('app.language')}
              </button>
              <button
                type="button"
                role="menuitem"
                className={`${styles.menuItem} ${styles.danger}`}
                onClick={() => {
                  setOpen(null);
                  logout();
                }}
              >
                <LogOut size={14} aria-hidden /> {t('app.signOut')}
              </button>
            </div>
          ) : null}
        </div>
      </div>

      {open ? (
        <button
          type="button"
          aria-hidden
          tabIndex={-1}
          className={styles.backdrop}
          onClick={() => setOpen(null)}
        />
      ) : null}
    </div>
  );
}
