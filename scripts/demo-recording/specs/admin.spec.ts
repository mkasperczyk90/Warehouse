import { test, expect, type Locator } from '@playwright/test';

import { registerOverlay } from '../lib/overlay';
import { beat, caption, humanClick, humanType, scan, scene } from '../lib/human';

/**
 * Admin half of the golden path (docs/demo-walkthrough.md scenes ① ② ④ ⑥ ⑦ ⑨ ⑩)
 * — Manager, Coordinator and Inspector taking turns at the desk, signing in/out
 * between acts. The admin serves its own MSW-mocked backend in dev (ADR-0006),
 * so this needs no live services.
 *
 * Write actions are "soft": if a button isn't where we expect, we narrate and
 * move on rather than abort the whole take — the recording is a tour, not a test.
 */
test('admin-golden-path', async ({ page }) => {
  await registerOverlay(page);
  await page.addInitScript(() => {
    try {
      window.localStorage.removeItem('wh.currentUser');
      window.localStorage.setItem('wms-locale', 'en');
    } catch {
      /* storage unavailable */
    }
  });

  const userMenuTrigger = page.getByRole('button', { name: /K\. Manager|A\. Coordinator|M\. Inspector/ });

  const softClick = async (target: Locator, fallback: string): Promise<boolean> => {
    try {
      await expect(target.first()).toBeVisible({ timeout: 6000 });
      await humanClick(page, target);
      return true;
    } catch {
      await caption(page, fallback);
      await beat(page, 700);
      return false;
    }
  };

  // Sidebar links are clicked by href so navigation is language-independent
  // (the labels localise to PL when a Polish-profile user signs in).
  const navTo = async (href: string) => {
    await humanClick(page, page.locator(`a[href="${href}"]`));
    await beat(page, 500);
  };

  // Dismiss the user-menu overlay (its backdrop intercepts clicks until closed).
  const closeMenu = async () => {
    const backdrop = page.locator('[class*="backdrop"]').first();
    if (await backdrop.isVisible().catch(() => false)) await backdrop.click({ force: true });
  };

  // The signed-in user's profile language drives the UI (a coordinator is PL),
  // so pin English after each sign-in. Detect the language from the Today nav
  // link's text ("Dziś" = Polish) so English actors need no menu fuss at all.
  const ensureEnglish = async () => {
    try {
      const todayText = await page.locator('a[href="/today"]').first().innerText().catch(() => 'Today');
      if (!/Dzi/i.test(todayText)) return; // already English
      await humanClick(page, userMenuTrigger);
      await humanClick(page, page.getByRole('menuitem', { name: /Language|Język/ }));
      await closeMenu();
      await beat(page, 300);
    } catch {
      /* leave the language as-is rather than abort the take */
    }
  };

  const signIn = async (badge: string, who: string) => {
    await scene(page, `Sign in — ${who}`, `Badge ${badge}`);
    await scan(page, page.getByRole('textbox').first(), badge);
    await expect(userMenuTrigger).toBeVisible();
    await ensureEnglish();
    await beat(page, 400);
  };

  const signOut = async () => {
    await humanClick(page, userMenuTrigger);
    await humanClick(page, page.getByRole('menuitem', { name: /Sign out|Wyloguj/ }));
    await expect(page.getByRole('button', { name: /Sign in|Zaloguj/ })).toBeVisible();
    await beat(page, 500);
  };

  // === Sign in: Warehouse Manager ===========================================
  await page.goto('/');
  await scene(page, 'Warehouse admin', 'Badge-scan sign-in (ADR-0006: MSW-mocked backend)');
  await signIn('1001', 'Warehouse Manager');

  // --- ① Master data — define a product ------------------------------------
  await scene(page, '① Define a product', 'Manager · Products');
  await navTo('/products');
  await expect(page).toHaveURL(/\/products(\?|$)/);
  if (await softClick(page.getByRole('button', { name: /Define product/ }), 'Catalogue of demo products')) {
    await caption(page, 'Fill the new product', 'SKU · name · category · base unit');
    await humanType(page, page.getByLabel('SKU', { exact: true }), 'DEMO-1');
    await humanType(page, page.getByLabel('Name', { exact: true }), 'Demo bar 50 g');
    await softClick(page.getByRole('button', { name: 'Create product' }), 'Create the product');
    await beat(page, 600);
  }
  await caption(page, 'Find it in the catalogue', 'Search "Demo"');
  await humanType(page, page.getByPlaceholder('Search name or SKU…'), 'Demo');
  await beat(page, 900);

  await signOut();

  // === Sign in: Logistics Coordinator =======================================
  await signIn('1002', 'Logistics Coordinator');

  // --- ② Announce a delivery (ASN) + dock slot -----------------------------
  await scene(page, '② Announce a delivery', 'Coordinator · Inbound (ASN)');
  await navTo('/inbound');
  await expect(page).toHaveURL(/\/inbound(\?|$)/);
  await caption(page, 'Assign a dock slot', 'Pick an announced ASN');
  await softClick(page.getByRole('button', { name: /2208/ }), 'Announced deliveries');
  if (await softClick(page.getByRole('button', { name: /Assign dock slot/ }), 'Dock slot assignment')) {
    await page.getByLabel('Dock', { exact: true }).selectOption({ index: 1 }).catch(() => {});
    await humanType(page, page.getByLabel('Time window'), '11:00–12:00');
    await softClick(page.getByRole('button', { name: 'Assign slot' }), 'Slot assigned');
    await beat(page, 600);
  }
  await caption(page, 'Mark the milk delivery arrived', 'ASN-2206 · Announced → Arrived');
  await softClick(page.getByRole('button', { name: /2206/ }), '');
  await softClick(page.getByRole('button', { name: 'Mark arrived' }), 'Truck at the dock → operator receives');

  await signOut();

  // === Sign in: Quality Inspector ===========================================
  await signIn('1003', 'Quality Inspector');

  // --- ④ Quality decision ---------------------------------------------------
  await scene(page, '④ Quality decision', 'Inspector · Quality holds');
  await navTo('/quality');
  await expect(page).toHaveURL(/\/quality(\?|$)/);
  await caption(page, 'Release a quarantined batch', 'Every decision needs an audited reason');
  if (await softClick(page.getByRole('button', { name: 'Release' }), 'Batches in quarantine')) {
    await page.getByLabel('Reason').selectOption({ index: 1 }).catch(() => {});
    await beat(page, 400);
    await softClick(page.getByRole('button', { name: 'Confirm release' }), 'Released → available to reservations');
  }

  await signOut();

  // === Sign in: Warehouse Manager (stock is live) ===========================
  await signIn('1001', 'Warehouse Manager');

  // --- ⑥ Stock is live ------------------------------------------------------
  await scene(page, '⑥ Stock is live', 'Manager · Stock view');
  await navTo('/stock');
  await expect(page).toHaveURL(/\/stock(\?|$)/);
  await caption(page, 'On hand · ATP · Reserved · Blocked', 'Status colour = domain status, never decoration');
  await beat(page, 1100);
  await caption(page, 'The immutable ledger', 'Movements — receipt & put-away as signed entries');
  await navTo('/movements');
  await expect(page).toHaveURL(/\/movements(\?|$)/);
  await beat(page, 1200);

  // --- ⑦ Create an outbound order ------------------------------------------
  await signOut();
  await signIn('1002', 'Logistics Coordinator');
  await scene(page, '⑦ Outbound order', 'Coordinator · Outbound');
  await navTo('/outbound');
  await expect(page).toHaveURL(/\/outbound(\?|$)/);
  await caption(page, 'Release a reserved order to a wave', 'StockReserved → pick task for the operator');
  await softClick(page.getByRole('button', { name: /4471|SO-/ }), 'Outbound orders');
  await softClick(page.getByRole('button', { name: 'Release to wave' }), 'Reserved → Picking');

  // --- ⑨ Dispatch to carrier ------------------------------------------------
  await scene(page, '⑨ Dispatch to carrier', 'Coordinator · Dispatch board');
  await navTo('/dispatch');
  await expect(page).toHaveURL(/\/dispatch(\?|$)/);
  await caption(page, 'Assign a carrier', 'Packed → Carrier assigned → Notice sent → Dispatched');
  if (await softClick(page.getByRole('button', { name: 'Assign carrier' }), 'Kanban: awaiting carrier')) {
    await page.getByLabel('Carrier', { exact: true }).selectOption({ index: 1 }).catch(() => {});
    await beat(page, 300);
    await softClick(page.getByRole('button', { name: 'Assign', exact: true }), 'Carrier assigned');
    await softClick(page.getByRole('button', { name: /Send pickup notice/ }), 'Pickup notice sent');
  }

  await signOut();

  // === ⑩ Close the loop · Manager ===========================================
  await signIn('1001', 'Warehouse Manager');
  await scene(page, '⑩ Close the loop', 'Manager · Today');
  await navTo('/today');
  await expect(page).toHaveURL(/\/today(\?|$)/);
  await caption(page, 'What needs you now', 'The worklist you worked has cleared — end of the golden path 🎬');
  await beat(page, 1400);
  await caption(page, '');
});
