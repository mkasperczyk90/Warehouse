import { useState, type FormEvent } from 'react';
import { ScanLine, Warehouse } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { useAuth } from '@/shared/auth/AuthContext';
import styles from './LoginScreen.module.css';

/**
 * Badge-scan sign-in. A hardware badge reader types the id and emits Enter, so
 * the single autofocused field + form submit handles both a scan and manual
 * entry. An unknown badge surfaces an inline error and clears the field.
 */
export function LoginScreen() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const [badge, setBadge] = useState('');
  const [error, setError] = useState(false);
  const [busy, setBusy] = useState(false);

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const value = badge.trim();
    if (!value || busy) return;
    setBusy(true);
    setError(false);
    try {
      await login(value);
    } catch {
      setError(true);
      setBadge('');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className={styles.screen}>
      <form className={styles.card} onSubmit={onSubmit}>
        <div className={styles.brand}>
          <Warehouse size={22} aria-hidden /> {t('app.name')}
        </div>
        <h1 className={styles.title}>{t('login.title')}</h1>
        <p className={styles.subtitle}>{t('login.subtitle')}</p>

        <label className={styles.field}>
          <span className={styles.label}>{t('login.badgeLabel')}</span>
          <span className={styles.inputWrap}>
            <ScanLine size={18} aria-hidden className={styles.scanIcon} />
            <input
              className={styles.input}
              value={badge}
              onChange={(e) => setBadge(e.target.value)}
              placeholder={t('login.badgePlaceholder')}
              autoFocus
              autoComplete="off"
              inputMode="numeric"
            />
          </span>
        </label>

        {error ? (
          <p className={styles.error} role="alert">
            {t('login.error')}
          </p>
        ) : null}

        <button type="submit" className={styles.submit} disabled={busy || !badge.trim()}>
          {t('login.submit')}
        </button>

        <p className={styles.hint}>{t('login.hint')}</p>
      </form>
    </div>
  );
}
