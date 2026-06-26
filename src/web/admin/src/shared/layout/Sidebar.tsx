import {
  ArrowDownToLine,
  ArrowLeftRight,
  ArrowUpRight,
  Boxes,
  ClipboardCheck,
  Diff,
  ListChecks,
  Network,
  ShieldCheck,
  Tags,
  Truck,
  Users,
  Warehouse,
  type LucideIcon,
} from 'lucide-react';
import { Link } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';

import { useWorklist, type WorklistCounts } from '@/features/Today';
import { ROUTES, type RoutePath } from '@/navigation/routes';
import styles from './Sidebar.module.css';

// Only routes the router actually registers are valid `<Link to>` targets;
// Partners is designed but unbuilt (no route, dimmed item).
type BuiltRoute = Exclude<RoutePath, typeof ROUTES.partners>;

interface NavItem {
  icon: LucideIcon;
  labelKey: string;
  /** Items without a route are designed but not yet built — dimmed, inert. */
  to?: BuiltRoute;
  /** Worklist count to badge (mirrors the Today landing). */
  countKey?: keyof WorklistCounts;
}

interface NavGroup {
  labelKey: string;
  items: NavItem[];
}

const GROUPS: NavGroup[] = [
  {
    labelKey: 'group.inventory',
    items: [
      { to: ROUTES.stock, icon: Boxes, labelKey: 'nav.stock' },
      { to: ROUTES.movements, icon: ArrowLeftRight, labelKey: 'nav.movements' },
      {
        to: ROUTES.stocktake,
        icon: ClipboardCheck,
        labelKey: 'nav.stocktake',
        countKey: 'stocktake',
      },
      { to: ROUTES.adjustment, icon: Diff, labelKey: 'nav.adjustment' },
      { to: ROUTES.quality, icon: ShieldCheck, labelKey: 'nav.quality', countKey: 'qc' },
    ],
  },
  {
    labelKey: 'group.logistics',
    items: [
      {
        to: ROUTES.inbound,
        icon: ArrowDownToLine,
        labelKey: 'nav.inbound',
        countKey: 'inbound',
      },
      {
        to: ROUTES.outbound,
        icon: ArrowUpRight,
        labelKey: 'nav.outbound',
        countKey: 'partial',
      },
      { to: ROUTES.dispatch, icon: Truck, labelKey: 'nav.dispatch' },
    ],
  },
  {
    labelKey: 'group.masterData',
    items: [
      { to: ROUTES.products, icon: Tags, labelKey: 'nav.products' },
      { to: ROUTES.topology, icon: Network, labelKey: 'nav.topology' },
      { icon: Users, labelKey: 'nav.partners' },
    ],
  },
];

const TONE: Record<keyof WorklistCounts, string> = {
  qc: styles.countRed,
  expiring: styles.countRed,
  partial: styles.countAmber,
  inbound: styles.countBlue,
  stocktake: styles.countBlue,
};

export function Sidebar() {
  const { t } = useTranslation();
  const counts = useWorklist().data?.counts;

  return (
    <aside className={styles.side}>
      <div className={styles.logo}>
        <Warehouse size={20} aria-hidden /> {t('app.name')}
      </div>

      <Link
        to={ROUTES.today}
        className={styles.item}
        activeProps={{ className: `${styles.item} ${styles.on}` }}
      >
        <ListChecks size={18} aria-hidden /> {t('nav.today')}
      </Link>

      {GROUPS.map((group) => (
        <div key={group.labelKey}>
          <div className={styles.group}>{t(group.labelKey)}</div>
          {group.items.map((item) => {
            const Icon = item.icon;
            const label = t(item.labelKey);
            const count = item.countKey && counts ? counts[item.countKey] : 0;
            const badge =
              item.countKey && count > 0 ? (
                <span className={`${styles.count} ${TONE[item.countKey]}`}>{count}</span>
              ) : null;

            if (!item.to) {
              return (
                <span key={item.labelKey} className={`${styles.item} ${styles.disabled}`}>
                  <Icon size={18} aria-hidden /> {label}
                </span>
              );
            }
            return (
              <Link
                key={item.labelKey}
                to={item.to}
                className={styles.item}
                activeProps={{ className: `${styles.item} ${styles.on}` }}
              >
                <Icon size={18} aria-hidden /> {label}
                {badge}
              </Link>
            );
          })}
        </div>
      ))}
    </aside>
  );
}
