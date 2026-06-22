/**
 * Warehouse WMS — Design tokens (Operator terminal scale)
 *
 * Direct port of docs/design/prototypes/tokens.css, restricted to the
 * `.terminal` type scale. Colour is never decoration here — it encodes
 * domain stock status (see docs/design/00-design-system.md).
 */

/** Semantic stock-status colours. They MUST mean the same as the domain. */
export const status = {
  available: { fg: '#2f9e44', bg: '#ebfbee' }, // on-hand, sellable      (green)
  reserved: { fg: '#1971c2', bg: '#e7f5ff' }, // spoken-for soft/hard    (blue)
  blocked: { fg: '#e03131', bg: '#fff0f0' }, // QC hold — never shippable (red)
  expired: { fg: '#a61e1e', bg: '#ffe3e3' }, // past best-before     (dark red)
  transit: { fg: '#f08c00', bg: '#fff4e6' }, // inter-warehouse move    (amber)
} as const;

export type StatusKey = keyof typeof status;

/** Neutral palette. */
export const color = {
  ink: '#1a1a1a',
  inkSoft: '#495057',
  inkFaint: '#868e96',
  line: '#dee2e6',
  lineSoft: '#f1f3f5',
  surface: '#ffffff',
  canvas: '#f8f9fa',
  brand: '#364fc7', // neutral indigo until a brand is chosen
  brandInk: '#ffffff',
  move: '#5f3dc4', // the violet used for the Move-stock task/flow
} as const;

/** Spacing scale — 4px base. */
export const s = {
  1: 4,
  2: 8,
  3: 12,
  4: 16,
  5: 20,
  6: 24,
  8: 32,
  10: 40,
  12: 48,
} as const;

/** Radius scale. */
export const radius = {
  sm: 6,
  md: 10,
  lg: 16,
  pill: 999,
} as const;

/**
 * Operator-terminal type scale (large; min tap target >= 48px).
 * Mirrors the `.terminal` block in tokens.css.
 */
export const fs = {
  xs: 15,
  sm: 17,
  md: 20,
  lg: 26,
  xl: 34,
  '2xl': 44,
} as const;

/** Minimum tap target for the terminal — hard floor 48px, design uses 56px. */
export const TAP = 56;

/**
 * Elevation — kept minimal: a freezer is no place for a 400ms ease.
 * Uses the cross-platform `boxShadow` style (RN 0.85+) rather than the
 * deprecated `shadow*` props, and mirrors the two shadows from tokens.css.
 */
export const shadow = {
  e1: { boxShadow: '0px 1px 2px rgba(0,0,0,0.06), 0px 1px 3px rgba(0,0,0,0.08)' },
  e2: { boxShadow: '0px 4px 12px rgba(0,0,0,0.10)' },
} as const;
