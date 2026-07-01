import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react';

import { color as lightColor, status as lightStatus } from './tokens';

export type Palette = Record<keyof typeof lightColor, string>;
export interface StatusTone {
  fg: string;
  bg: string;
}
export type StatusPalette = Record<keyof typeof lightStatus, StatusTone>;

export interface Theme {
  hc: boolean;
  color: Palette;
  status: StatusPalette;
}

/**
 * High-contrast (glare) palette. A bright cold-store floor washes out faint
 * greys and the lighter status hues (amber on a light tint fails WCAG). This
 * variant darkens every foreground so it passes AA on white, kills the faint
 * greys, and makes lines solid. Mirrors the `.hc` block in tokens.css.
 */
const hcColor: Palette = {
  ink: '#000000',
  inkSoft: '#1a1a1a',
  inkFaint: '#333333',
  line: '#111111',
  lineSoft: '#999999',
  surface: '#ffffff',
  canvas: '#ffffff',
  brand: '#00308f',
  brandInk: '#ffffff',
  move: '#4a2fa8',
};

const hcStatus: StatusPalette = {
  available: { fg: '#1b6e2e', bg: '#d6f5dd' },
  reserved: { fg: '#11508f', bg: '#d4e8fb' },
  blocked: { fg: '#b71717', bg: '#ffdede' },
  expired: { fg: '#7d0f0f', bg: '#ffd0d0' },
  transit: { fg: '#8a4d00', bg: '#ffe6c2' },
};

export const lightTheme: Theme = { hc: false, color: lightColor, status: lightStatus };
export const hcTheme: Theme = { hc: true, color: hcColor, status: hcStatus };

interface ThemeContextValue {
  theme: Theme;
  hc: boolean;
  toggle: () => void;
}

const ThemeContext = createContext<ThemeContextValue>({
  theme: lightTheme,
  hc: false,
  toggle: () => {},
});

const STORAGE_KEY = 'wms-hc';

/** Persist per device. localStorage exists on web; absent (and harmless) on native. */
function loadHc(): boolean {
  try {
    return typeof localStorage !== 'undefined' && localStorage.getItem(STORAGE_KEY) === '1';
  } catch {
    return false;
  }
}
function saveHc(value: boolean): void {
  try {
    if (typeof localStorage !== 'undefined') localStorage.setItem(STORAGE_KEY, value ? '1' : '0');
  } catch {
    /* ignore — storage unavailable */
  }
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [hc, setHc] = useState(loadHc);
  const toggle = useCallback(() => {
    setHc((v) => {
      const next = !v;
      saveHc(next);
      return next;
    });
  }, []);
  const value = useMemo<ThemeContextValue>(
    () => ({ theme: hc ? hcTheme : lightTheme, hc, toggle }),
    [hc, toggle],
  );
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

/** The active theme (light or high-contrast). */
export const useTheme = (): Theme => useContext(ThemeContext).theme;

/** The HC flag + toggle, for the bar control. */
export const useThemeToggle = (): { hc: boolean; toggle: () => void } => {
  const { hc, toggle } = useContext(ThemeContext);
  return { hc, toggle };
};

/**
 * Build themed styles. `make` is a module-level constant, so the memo only
 * recomputes when the theme actually changes.
 */
export function useThemedStyles<T>(make: (t: Theme) => T): T {
  const theme = useTheme();
  return useMemo(() => make(theme), [theme, make]);
}
