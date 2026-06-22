import type { Href } from 'expo-router';

import { api } from '@/core/api/client';
import { ROUTES } from '@/navigation/routes';
import type { IconName } from '@/shared/ui';
import type { StatusKey } from '@/shared/theme/tokens';

export type ScannedKind = 'location' | 'lpn' | 'product' | 'asn' | 'order' | 'unknown';

export interface ScanResult {
  kind: ScannedKind;
  code: string;
  icon: IconName;
  /** i18n key for the entity type label (resolved in the screen). */
  titleKey: string;
  /** A believable detail line for the resolved entity (data; not localized). */
  subtitle: string;
  status?: { kind: StatusKey };
  /** The task this scan dispatches to: an i18n key for the label + the route. */
  action?: { key: string; route: Href };
}

/**
 * Mock dispatcher — resolves a scanned code to a domain entity by its shape
 * and points the operator at the matching task. The real terminal would ask
 * the Gateway "what is this code?"; here we pattern-match the ubiquitous
 * language (ASN-…, SO-…/W-…, an EAN, an LPN, a location address).
 */
export function resolveScan(raw: string): ScanResult {
  const code = raw.trim().toUpperCase();

  if (/^ASN-/.test(code)) {
    return {
      kind: 'asn',
      code,
      icon: 'asn',
      titleKey: 'scan.kind.asn',
      subtitle: 'Dairy Farms Ltd · Dock D-3',
      action: { key: 'scan.action.receive', route: ROUTES.receive },
    };
  }

  if (/^(SO|W)-/.test(code)) {
    return {
      kind: 'order',
      code,
      icon: 'order',
      titleKey: code.startsWith('W') ? 'scan.kind.wave' : 'scan.kind.order',
      subtitle: 'Wave W-2206 · 31 lines',
      action: { key: 'scan.action.pick', route: ROUTES.pick },
    };
  }

  if (/^\d{8,14}$/.test(code)) {
    return {
      kind: 'product',
      code,
      icon: 'product',
      titleKey: 'scan.kind.product',
      subtitle: 'Whole milk 3.2% — 1 L carton',
      status: { kind: 'available' },
      action: { key: 'scan.action.lookup', route: ROUTES.lookup },
    };
  }

  if (/^\d{3,4}-\d{4}-\d{3}$/.test(code)) {
    return {
      kind: 'lpn',
      code,
      icon: 'pallet',
      titleKey: 'scan.kind.lpn',
      subtitle: 'Whole milk 3.2% · 240 ea · LOT-0425-A',
      status: { kind: 'available' },
      action: { key: 'scan.action.putaway', route: ROUTES.putaway },
    };
  }

  // Location address: letters + digits in dash-separated segments.
  if (/^[A-Z]{1,4}\d*(-[A-Z0-9]+){2,}$/.test(code)) {
    return {
      kind: 'location',
      code,
      icon: 'location',
      titleKey: 'scan.kind.location',
      subtitle: 'Cold room 2–6 °C · 0.8 m³ free',
      action: { key: 'scan.action.move', route: ROUTES.move },
    };
  }

  return {
    kind: 'unknown',
    code,
    icon: 'unknown',
    titleKey: 'scan.kind.unknown',
    subtitle: '',
  };
}

/**
 * Seeded scan history (newest first) for the empty terminal. The Gateway returns
 * the raw codes; the dispatcher resolves each one to its entity client-side.
 */
export const getRecentScans = async (): Promise<ScanResult[]> => {
  const codes = await api.get<string[]>('scan/recent');
  return codes.map(resolveScan);
};
