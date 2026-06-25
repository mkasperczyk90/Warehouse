import {
  createRootRoute,
  createRoute,
  createRouter,
  redirect,
} from '@tanstack/react-router';

import { AppShell } from '@/shared/layout/AppShell';
import { TodayScreen } from '@/features/Today';
import { MovementsScreen } from '@/features/Movements';
import { StockScreen, StockItemRoute } from '@/features/Stock';
import { InboundScreen, ReceivingRoute } from '@/features/Inbound';
import { OutboundScreen } from '@/features/Outbound';
import { DispatchScreen } from '@/features/Dispatch';
import { StocktakeListScreen, StocktakeReviewRoute } from '@/features/Stocktake';
import { AdjustmentScreen, AdjustmentRoute } from '@/features/Adjustment';
import { QualityScreen } from '@/features/Quality';
import { ProductCatalogScreen, ProductDetailRoute } from '@/features/Products';
import { TopologyScreen } from '@/features/Topology';
import { ProfileScreen } from '@/features/Profile';
import { ROUTES } from '@/navigation/routes';

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
  component: TodayScreen,
});

const stockRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.stock,
  component: StockScreen,
});

const stockItemRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/stock/$id',
  component: StockItemRoute,
});

const movementsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.movements,
  component: MovementsScreen,
});

const inboundRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.inbound,
  component: InboundScreen,
});

const receivingRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/inbound/$id/receiving',
  component: ReceivingRoute,
});

const outboundRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.outbound,
  component: OutboundScreen,
});

const dispatchRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.dispatch,
  component: DispatchScreen,
});

const stocktakeRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.stocktake,
  component: StocktakeListScreen,
});

const stocktakeReviewRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/stocktake/$id',
  component: StocktakeReviewRoute,
});

const adjustmentRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.adjustment,
  component: AdjustmentScreen,
});

const adjustmentItemRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/adjustment/$itemId',
  component: AdjustmentRoute,
});

const qualityRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.quality,
  component: QualityScreen,
});

const productsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.products,
  component: ProductCatalogScreen,
});

const productDetailRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/products/$sku',
  component: ProductDetailRoute,
});

const topologyRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.topology,
  component: TopologyScreen,
});

const profileRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: ROUTES.profile,
  component: ProfileScreen,
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
