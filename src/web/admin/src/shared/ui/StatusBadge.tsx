/**
 * StatusBadge — dot + label pill. Status is never conveyed by colour alone
 * (a11y): the label is always present. Variants map to the load-bearing status
 * tokens in tokens.css (`.badge--*`). Shared with the terminal in spirit.
 */
export type StatusVariant = 'available' | 'reserved' | 'blocked' | 'expired' | 'transit';

export function StatusBadge({ variant, label }: { variant: StatusVariant; label: string }) {
  return <span className={`badge badge--${variant}`}>{label}</span>;
}
