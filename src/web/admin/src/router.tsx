import {
  createRootRoute,
  createRoute,
  createRouter,
  lazyRouteComponent,
  redirect,
} from '@tanstack/react-router';

import { AppShell } from '@/shared/layout/AppShell';
import { ROUTES } from '@/navigation/routes';
import { validateSelectionSearch } from '@/navigation/search';
import { validateStockSearch } from '@/features/Stock/stock.search';

// Each screen is loaded on demand (its own chunk) — the initial bundle is just
// the shell + router. `defaultPreload: 'intent'` warms the chunk on hover/focus,
// so navigation still feels instant. The shell stays eager (it frames everything).
const lazy = lazyRouteComponent;

const rootRoute = createRootRoute({ component: AppShell });

// `/` lands the desk on the worklist — "what needs me now" (admin-10, blog #20).
// Reverting to the stock view as the default is a one-line change (open PO call).
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  beforeLoad: () => {
    throw redirect({ to: ROUTES.today });
  },
});

const todayRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.today,
  component: lazy(() => import('@/features/Today'), 'TodayScreen'),
});

const stockRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.stock,
  validateSearch: validateStockSearch,
  component: lazy(() => import('@/features/Stock'), 'StockScreen'),
});

const stockItemRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/stock/$id',
  component: lazy(() => import('@/features/Stock'), 'StockItemRoute'),
});

const movementsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.movements,
  component: lazy(() => import('@/features/Movements'), 'MovementsScreen'),
});

const inboundRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.inbound,
  validateSearch: validateSelectionSearch,
  component: lazy(() => import('@/features/Inbound'), 'InboundScreen'),
});

const receivingRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/inbound/$id/receiving',
  component: lazy(() => import('@/features/Inbound'), 'ReceivingRoute'),
});

const outboundRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.outbound,
  validateSearch: validateSelectionSearch,
  component: lazy(() => import('@/features/Outbound'), 'OutboundScreen'),
});

const dispatchRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.dispatch,
  component: lazy(() => import('@/features/Dispatch'), 'DispatchScreen'),
});

const stocktakeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.stocktake,
  component: lazy(() => import('@/features/Stocktake'), 'StocktakeListScreen'),
});

const stocktakeReviewRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/stocktake/$id',
  component: lazy(() => import('@/features/Stocktake'), 'StocktakeReviewRoute'),
});

const adjustmentRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.adjustment,
  component: lazy(() => import('@/features/Adjustment'), 'AdjustmentScreen'),
});

const adjustmentItemRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/adjustment/$itemId',
  component: lazy(() => import('@/features/Adjustment'), 'AdjustmentRoute'),
});

const qualityRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.quality,
  component: lazy(() => import('@/features/Quality'), 'QualityScreen'),
});

const productsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.products,
  component: lazy(() => import('@/features/Products'), 'ProductCatalogScreen'),
});

const productDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/products/$sku',
  component: lazy(() => import('@/features/Products'), 'ProductDetailRoute'),
});

const topologyRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.topology,
  validateSearch: validateSelectionSearch,
  component: lazy(() => import('@/features/Topology'), 'TopologyScreen'),
});

const profileRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.profile,
  component: lazy(() => import('@/features/Profile'), 'ProfileScreen'),
});

const routeTree = rootRoute.addChildren([
  indexRoute,
  todayRoute,
  stockRoute,
  stockItemRoute,
  movementsRoute,
  inboundRoute,
  receivingRoute,
  outboundRoute,
  dispatchRoute,
  stocktakeRoute,
  stocktakeReviewRoute,
  adjustmentRoute,
  adjustmentItemRoute,
  qualityRoute,
  productsRoute,
  productDetailRoute,
  topologyRoute,
  profileRoute,
]);

export const router = createRouter({
  routeTree,
  defaultPreload: 'intent',
});

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
