import { test as base } from 'playwright-bdd';

import { installApiMocks } from './apiMocks';
import { GoodsReceiptPage } from '../e2e/pages/GoodsReceiptPage';
import { LanguagePage } from '../e2e/pages/LanguagePage';
import { LoginPage } from '../e2e/pages/LoginPage';
import { LookupPage } from '../e2e/pages/LookupPage';
import { MovePage } from '../e2e/pages/MovePage';
import { PackingPage } from '../e2e/pages/PackingPage';
import { PickingPage } from '../e2e/pages/PickingPage';
import { PutAwayPage } from '../e2e/pages/PutAwayPage';
import { ScanPage } from '../e2e/pages/ScanPage';
import { TabBar } from '../e2e/pages/TabBar';
import { TaskHubPage } from '../e2e/pages/TaskHubPage';

/**
 * Dependency injection for the BDD steps — each Page Object is a fixture so
 * step definitions just declare what they need (`{ taskHub }`) and get a
 * ready, page-bound instance. Build the custom `test` from playwright-bdd's
 * base so `createBdd(test)` wires the steps to these fixtures.
 */
export type TerminalFixtures = {
  taskHub: TaskHubPage;
  goodsReceipt: GoodsReceiptPage;
  putAway: PutAwayPage;
  picking: PickingPage;
  packing: PackingPage;
  move: MovePage;
  scan: ScanPage;
  lookup: LookupPage;
  tabBar: TabBar;
  language: LanguagePage;
  login: LoginPage;
};

export const test = base.extend<TerminalFixtures>({
  /**
   * Pin a deterministic locale + theme before the app boots. The terminal
   * defaults to Polish (floor staff) and remembers the high-contrast choice;
   * this English-authored suite pins **English** + light theme so the copy
   * assertions stay stable. `addInitScript` runs before the app's first
   * render — which is when the i18n/theme providers read localStorage. The
   * dedicated language test drives the in-app toggle to exercise Polish.
   *
   * The terminal also gates on a signed-in operator (badge scan), so we seed a
   * stored session here — the persisted user (AuthContext re-arms the api seam's
   * token + warehouse from it) plus the bearer token — otherwise every screen
   * test would land on the login screen. Scenarios tagged `@anonymous` opt out
   * of the seed to drive the sign-in flow itself.
   *
   * The app always calls the real Gateway, so we intercept `/api` with deterministic
   * stubs (see apiMocks) before the first render fetches.
   */
  page: async ({ page }, use, testInfo) => {
    const authenticated = !testInfo.tags.includes('@anonymous');
    await installApiMocks(page);
    await page.addInitScript((seedOperator) => {
      try {
        window.localStorage.setItem('wms-locale', 'en');
        window.localStorage.setItem('wms-hc', '0');
        if (seedOperator) {
          window.localStorage.setItem(
            'wms-operator',
            JSON.stringify({
              id: 'OP-7700',
              badge: '7700',
              name: 'M. Operator',
              role: 'operator',
              email: '',
              defaultWarehouseId: 'WH01',
              language: 'en',
            }),
          );
          window.localStorage.setItem('wms-token', 'e2e-token');
        } else {
          window.localStorage.removeItem('wms-operator');
          window.localStorage.removeItem('wms-token');
        }
      } catch {
        /* storage unavailable — fall back to app defaults */
      }
    }, authenticated);
    await use(page);
  },

  taskHub: async ({ page }, use) => {
    await use(new TaskHubPage(page));
  },
  goodsReceipt: async ({ page }, use) => {
    await use(new GoodsReceiptPage(page));
  },
  putAway: async ({ page }, use) => {
    await use(new PutAwayPage(page));
  },
  picking: async ({ page }, use) => {
    await use(new PickingPage(page));
  },
  packing: async ({ page }, use) => {
    await use(new PackingPage(page));
  },
  move: async ({ page }, use) => {
    await use(new MovePage(page));
  },
  scan: async ({ page }, use) => {
    await use(new ScanPage(page));
  },
  lookup: async ({ page }, use) => {
    await use(new LookupPage(page));
  },
  tabBar: async ({ page }, use) => {
    await use(new TabBar(page));
  },
  language: async ({ page }, use) => {
    await use(new LanguagePage(page));
  },
  login: async ({ page }, use) => {
    await use(new LoginPage(page));
  },
});

export { expect } from '@playwright/test';
