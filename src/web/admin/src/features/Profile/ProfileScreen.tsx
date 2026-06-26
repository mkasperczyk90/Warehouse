import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';

import { useAuth } from '@/shared/auth/AuthContext';
import { useWarehouses, warehouseLabel } from '@/features/Warehouses';
import { useProfile, useUpdateProfile, type ProfilePrefs } from './profile.model';
import styles from './ProfileScreen.module.css';

const LANGUAGES: ProfilePrefs['language'][] = ['en', 'pl'];

export function ProfileScreen() {
  const { t } = useTranslation();
  const { user, updateUser } = useAuth();
  const queryClient = useQueryClient();

  const profile = useProfile(user?.id);
  const warehouses = useWarehouses();
  const save = useUpdateProfile(user?.id);

  const [form, setForm] = useState<ProfilePrefs | null>(null);
  const [saved, setSaved] = useState(false);

  // Seed the editable form once the record arrives.
  useEffect(() => {
    if (profile.data) {
      setForm({
        phone: profile.data.phone,
        defaultWarehouseId: profile.data.defaultWarehouseId,
        language: profile.data.language,
      });
    }
  }, [profile.data]);

  if (profile.isLoading || !form) return <p className={styles.state}>{t('state.loading')}</p>;
  if (profile.isError || !profile.data) return <p className={styles.state}>{t('state.error')}</p>;

  const p = profile.data;
  const initials = p.name
    .split(/\s+/)
    .map((s) => s[0])
    .slice(0, 2)
    .join('')
    .toUpperCase();

  const onSave = () => {
    save.mutate(form, {
      onSuccess: (updated) => {
        if (user) {
          updateUser({
            ...user,
            defaultWarehouseId: updated.defaultWarehouseId,
            language: updated.language,
          });
        }
        void queryClient.invalidateQueries({ queryKey: ['profile', p.id] });
        setSaved(true);
      },
    });
  };

  return (
    <>
      <div className={styles.head}>
        <span className={styles.avatar}>{initials}</span>
        <div>
          <h2 className={styles.title}>{p.name}</h2>
          <div className={styles.role}>{t(`profile.role.${p.role}`)}</div>
        </div>
      </div>

      <div className={styles.grid}>
        <section className={styles.panel}>
          <h3 className={styles.panelTitle}>{t('profile.identity')}</h3>
          <dl className={styles.facts}>
            <dt>{t('profile.badge')}</dt>
            <dd>{p.badge}</dd>
            <dt>{t('profile.email')}</dt>
            <dd>{p.email}</dd>
            <dt>{t('profile.role.label')}</dt>
            <dd>{t(`profile.role.${p.role}`)}</dd>
          </dl>
        </section>

        <section className={styles.panel}>
          <h3 className={styles.panelTitle}>{t('profile.preferences')}</h3>

          <label className={styles.field}>
            <span className={styles.label}>{t('profile.phone')}</span>
            <input
              className={styles.input}
              value={form.phone}
              onChange={(e) => {
                setForm({ ...form, phone: e.target.value });
                setSaved(false);
              }}
            />
          </label>

          <label className={styles.field}>
            <span className={styles.label}>{t('profile.defaultWarehouse')}</span>
            <select
              className={styles.input}
              value={form.defaultWarehouseId}
              onChange={(e) => {
                setForm({ ...form, defaultWarehouseId: e.target.value });
                setSaved(false);
              }}
            >
              {(warehouses.data ?? []).map((w) => (
                <option key={w.id} value={w.id}>
                  {warehouseLabel(w)}
                </option>
              ))}
            </select>
          </label>

          <label className={styles.field}>
            <span className={styles.label}>{t('profile.language')}</span>
            <select
              className={styles.input}
              value={form.language}
              onChange={(e) => {
                setForm({
                  ...form,
                  language: e.target.value as ProfilePrefs['language'],
                });
                setSaved(false);
              }}
            >
              {LANGUAGES.map((lng) => (
                <option key={lng} value={lng}>
                  {t(`profile.languageOpt.${lng}`)}
                </option>
              ))}
            </select>
          </label>

          <div className={styles.actions}>
            {saved ? <span className={styles.saved}>{t('profile.saved')}</span> : null}
            <button
              type="button"
              className={styles.save}
              onClick={onSave}
              disabled={save.isPending}
            >
              {t('profile.save')}
            </button>
          </div>
        </section>

        <section className={styles.panel}>
          <h3 className={styles.panelTitle}>{t('profile.activity')}</h3>
          <div className={styles.lastLogin}>
            {t('profile.lastLogin')}: <b>{p.lastLogin}</b>
          </div>
          <ul className={styles.sessions}>
            {p.recentSessions.map((s) => (
              <li key={s.id} className={styles.session}>
                <span>{s.device}</span>
                <span className={styles.sub}>{s.when}</span>
              </li>
            ))}
          </ul>
        </section>
      </div>
    </>
  );
}
