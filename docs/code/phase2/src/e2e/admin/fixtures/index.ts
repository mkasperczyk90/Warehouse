import { test as base } from 'playwright-bdd';

import { AdjustmentPage } from '../e2e/pages/AdjustmentPage';
import { AppShell } from '../e2e/pages/AppShell';
import { DispatchPage } from '../e2e/pages/DispatchPage';
import { InboundPage } from '../e2e/pages/InboundPage';
import { MovementsPage } from '../e2e/pages/MovementsPage';
import { OutboundPage } from '../e2e/pages/OutboundPage';
import { ProductsPage } from '../e2e/pages/ProductsPage';
import { QualityPage } from '../e2e/pages/QualityPage';
import { SearchPage } from '../e2e/pages/SearchPage';
import { StockItemPage } from '../e2e/pages/StockItemPage';
import { StockPage } from '../e2e/pages/StockPage';
import { StocktakePage } from '../e2e/pages/StocktakePage';
import { TodayPage } from '../e2e/pages/TodayPage';
import { TopologyPage } from '../e2e/pages/TopologyPage';

/**
 * Dependency injection for the BDD steps — each Page Object is a fixture so
 * step definitions just declare what they need (`{ stock }`) and get a ready,
 * page-bound instance. Build the custom `test` from playwright-bdd's base so
 * `createBdd(test)` wires the steps to these fixtures.
 *
 * Unlike the terminal, the admin always boots in English (i18n `lng: 'en'`,
 * no persisted locale), so no locale pinning is needed — the in-app PL/EN
 * toggle is exercised directly by `language.feature`.
 */
export type AdminFixtures = {
  app: AppShell;
  today: TodayPage;
  stock: StockPage;
  stockItem: StockItemPage;
  search: SearchPage;
  inbound: InboundPage;
  quality: QualityPage;
  stocktake: StocktakePage;
  adjustment: AdjustmentPage;
  outbound: OutboundPage;
  dispatch: DispatchPage;
  products: ProductsPage;
  topology: TopologyPage;
  movements: MovementsPage;
};

export const test = base.extend<AdminFixtures>({
  app: async ({ page }, use) => {
    await use(new AppShell(page));
  },
  today: async ({ page }, use) => {
    await use(new TodayPage(page));
  },
  stock: async ({ page }, use) => {
    await use(new StockPage(page));
  },
  stockItem: async ({ page }, use) => {
    await use(new StockItemPage(page));
  },
  search: async ({ page }, use) => {
    await use(new SearchPage(page));
  },
  inbound: async ({ page }, use) => {
    await use(new InboundPage(page));
  },
  quality: async ({ page }, use) => {
    await use(new QualityPage(page));
  },
  stocktake: async ({ page }, use) => {
    await use(new StocktakePage(page));
  },
  adjustment: async ({ page }, use) => {
    await use(new AdjustmentPage(page));
  },
  outbound: async ({ page }, use) => {
    await use(new OutboundPage(page));
  },
  dispatch: async ({ page }, use) => {
    await use(new DispatchPage(page));
  },
  products: async ({ page }, use) => {
    await use(new ProductsPage(page));
  },
  topology: async ({ page }, use) => {
    await use(new TopologyPage(page));
  },
  movements: async ({ page }, use) => {
    await use(new MovementsPage(page));
  },
});

export { expect } from '@playwright/test';
