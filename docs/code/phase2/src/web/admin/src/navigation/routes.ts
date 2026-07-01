/**
 * The typed path map — single source of truth for admin routes (mirrors the
 * terminal's `navigation/routes.ts`). The TanStack router in `src/router.tsx`
 * registers these; components and the sidebar navigate by name, not literals.
 */
export const ROUTES = {
  today: '/today',
  stock: '/stock',
  movements: '/movements',
  stocktake: '/stocktake',
  adjustment: '/adjustment',
  quality: '/quality',
  inbound: '/inbound',
  outbound: '/outbound',
  dispatch: '/dispatch',
  products: '/products',
  topology: '/topology',
  partners: '/partners',
} as const;

export type RoutePath = (typeof ROUTES)[keyof typeof ROUTES];

/** Per-route breadcrumb metadata (i18n keys, resolved at render with `t`). */
export const ROUTE_META: Record<string, { groupKey: string; titleKey: string }> = {
  [ROUTES.today]: { groupKey: '', titleKey: 'nav.today' },
  [ROUTES.stock]: { groupKey: 'group.inventory', titleKey: 'nav.stock' },
  [ROUTES.movements]: { groupKey: 'group.inventory', titleKey: 'nav.movements' },
  [ROUTES.stocktake]: { groupKey: 'group.inventory', titleKey: 'nav.stocktake' },
  [ROUTES.adjustment]: { groupKey: 'group.inventory', titleKey: 'nav.adjustment' },
  [ROUTES.quality]: { groupKey: 'group.inventory', titleKey: 'nav.quality' },
  [ROUTES.inbound]: { groupKey: 'group.logistics', titleKey: 'nav.inbound' },
  [ROUTES.outbound]: { groupKey: 'group.logistics', titleKey: 'nav.outbound' },
  [ROUTES.dispatch]: { groupKey: 'group.logistics', titleKey: 'nav.dispatch' },
  [ROUTES.products]: { groupKey: 'group.masterData', titleKey: 'nav.products' },
  [ROUTES.topology]: { groupKey: 'group.masterData', titleKey: 'nav.topology' },
  [ROUTES.partners]: { groupKey: 'group.masterData', titleKey: 'nav.partners' },
};
