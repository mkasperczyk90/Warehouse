import { MaterialCommunityIcons } from '@expo/vector-icons';

import { color } from '@/shared/theme/tokens';

/**
 * Icon — the terminal's icon set, by semantic name.
 *
 * Replaces the earlier bare Unicode glyphs (▣ ⇩ ▦ ⇄), which render
 * inconsistently and risk "tofu" boxes on rugged Android handhelds. Callers use
 * a domain/action name; the mapping to the underlying icon font lives here, so
 * swapping the icon set later touches one file.
 */
const MAP = {
  // actions / navigation
  scan: 'barcode-scan',
  receive: 'tray-arrow-down',
  putaway: 'package-up',
  pick: 'package-down',
  move: 'swap-horizontal',
  tasks: 'format-list-checks',
  search: 'magnify',
  more: 'dots-horizontal',
  back: 'arrow-left',
  print: 'printer',
  contrast: 'contrast-circle',
  // domain entities (scan dispatcher / look-up)
  product: 'cube-outline',
  location: 'map-marker-outline',
  pallet: 'package-variant-closed',
  batch: 'calendar-clock',
  asn: 'truck-delivery-outline',
  order: 'tray-arrow-up',
  unknown: 'help-circle-outline',
} as const;

export type IconName = keyof typeof MAP;

export function Icon({
  name,
  size = 24,
  color: tint = color.ink,
}: {
  name: IconName;
  size?: number;
  color?: string;
}) {
  return <MaterialCommunityIcons name={MAP[name]} size={size} color={tint} />;
}
