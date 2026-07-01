import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';

import { en } from './en';
import { pl } from './pl';

export type Locale = 'pl' | 'en';
type Dict = Record<string, string>;

const DICTS: Record<Locale, Dict> = { pl, en };

export type TParams = Record<string, string | number>;

function interpolate(template: string, params?: TParams): string {
  if (!params) return template;
  return template.replace(/\{(\w+)\}/g, (_, k: string) =>
    k in params ? String(params[k]) : `{${k}}`,
  );
}

interface I18nContextValue {
  locale: Locale;
  setLocale: (l: Locale) => void;
  toggle: () => void;
  t: (key: string, params?: TParams) => string;
}

const I18nContext = createContext<I18nContextValue>({
  locale: 'pl',
  setLocale: () => {},
  toggle: () => {},
  t: (key) => key,
});

const STORAGE_KEY = 'wms-locale';

/** Default to Polish — the terminal is for Polish floor staff. Persist per device. */
function loadLocale(): Locale {
  try {
    const v = typeof localStorage !== 'undefined' ? localStorage.getItem(STORAGE_KEY) : null;
    return v === 'en' || v === 'pl' ? v : 'pl';
  } catch {
    return 'pl';
  }
}
function saveLocale(l: Locale): void {
  try {
    if (typeof localStorage !== 'undefined') localStorage.setItem(STORAGE_KEY, l);
  } catch {
    /* ignore — storage unavailable */
  }
}

export function I18nProvider({ children }: { children: ReactNode }) {
  const [locale, setLocaleState] = useState<Locale>(loadLocale);

  const setLocale = useCallback((l: Locale) => {
    setLocaleState(l);
    saveLocale(l);
  }, []);
  const toggle = useCallback(() => {
    setLocaleState((cur) => {
      const next: Locale = cur === 'pl' ? 'en' : 'pl';
      saveLocale(next);
      return next;
    });
  }, []);
  const t = useCallback(
    (key: string, params?: TParams) =>
      interpolate(DICTS[locale][key] ?? DICTS.en[key] ?? key, params),
    [locale],
  );

  const value = useMemo<I18nContextValue>(
    () => ({ locale, setLocale, toggle, t }),
    [locale, setLocale, toggle, t],
  );
  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export const useI18n = (): I18nContextValue => useContext(I18nContext);
export const useT = (): I18nContextValue['t'] => useContext(I18nContext).t;
