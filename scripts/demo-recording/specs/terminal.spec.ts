import { test, expect } from '@playwright/test';

import { installApiMocks } from '../lib/apiMocks';
import { registerOverlay } from '../lib/overlay';
import { beat, caption, humanClick, scan, scene } from '../lib/human';

/**
 * Terminal half of the golden path (docs/demo-walkthrough.md scenes ③ ⑤ ⑧ + the
 * Move side scene) — the Warehouse Operator on the handheld: badge in, receive,
 * put away, pick → pack, replenish. The Gateway is stubbed at the network layer
 * (see lib/apiMocks) so the run is deterministic and needs no backend.
 */
test('terminal-golden-path', async ({ page }) => {
  await installApiMocks(page);
  await registerOverlay(page);
  // Pin English + light theme, and start signed-out so we can show the badge scan.
  await page.addInitScript(() => {
    try {
      window.localStorage.setItem('wms-locale', 'en');
      window.localStorage.setItem('wms-hc', '0');
      window.localStorage.removeItem('wms-operator');
      window.localStorage.removeItem('wms-token');
    } catch {
      /* storage unavailable */
    }
  });

  const receivePile = page.getByRole('button', { name: /Receive/ }).first();
  const waitForHub = async () => {
    await expect(receivePile).toBeVisible();
    await beat(page, 700);
  };

  // --- Sign in (badge scan) -------------------------------------------------
  await page.goto('/');
  await scene(page, 'Operator terminal', 'Badge in to start the shift — scan 7700');
  await scan(page, page.getByPlaceholder('Scan badge or type number…'), '7700');
  await waitForHub();

  await scene(page, '③ Task hub', 'Receive · Put away · Pick · Move — counts live from the backend');

  // --- ③ Receive the delivery ----------------------------------------------
  await caption(page, '③ Receive the delivery', 'Open the Receive pile');
  await humanClick(page, receivePile);
  await expect(page).toHaveURL(/\/receive(\?|$)/);
  await scene(page, 'Goods receipt', 'ASN-2206 · MILK-1L · counted qty pre-filled to 240');
  await caption(page, 'Match the physical count', '+ / – stepper');
  await humanClick(page, page.getByLabel('increase'));
  await humanClick(page, page.getByLabel('decrease'));
  await caption(page, 'Confirm the line', 'GoodsReceived → a Put away task appears');
  await humanClick(page, page.getByRole('button', { name: /Confirm line/ }));
  await waitForHub();

  // --- ⑤ Put away the pallet -----------------------------------------------
  await caption(page, '⑤ Put away the pallet', 'Open the Put away pile');
  await humanClick(page, page.getByRole('button', { name: /Put away/ }));
  await expect(page).toHaveURL(/\/putaway(\?|$)/);
  await scene(page, 'Proposed bay', 'Temperature-compatible & capacity OK — the invariant in action');
  await caption(page, 'Confirm put-away', 'Posts a PutAway move to the immutable ledger');
  await humanClick(page, page.getByRole('button', { name: /Confirm put-away/ }));
  await waitForHub();

  // --- ⑧ Pick → Pack --------------------------------------------------------
  await caption(page, '⑧ Pick → Pack', 'Open the Pick pile');
  await humanClick(page, page.getByRole('button', { name: /Pick/ }));
  await expect(page).toHaveURL(/\/pick(\?|$)/);
  await scene(page, 'Picking', 'Go-to location · FEFO batch — confirm is gated until both scans land');
  const scanField = page.getByRole('textbox').first();
  await caption(page, 'Scan the location', '');
  await scan(page, scanField, 'WH01-A2-A09-R1-S2');
  await caption(page, 'Scan the product', 'Now Confirm pick unlocks');
  await scan(page, scanField, '4006381333931');
  await humanClick(page, page.getByRole('button', { name: /Confirm pick/ }));

  await expect(page).toHaveURL(/\/pack(\?|$)/);
  await scene(page, 'Packing opens automatically', 'PKG 1 — the picked lines for the order');
  await caption(page, 'Close the package', 'Order marked packed — ready for the carrier');
  await humanClick(page, page.getByRole('button', { name: /Close package/ }));
  await waitForHub();

  // --- Side scene · Move stock (replenishment) ------------------------------
  await caption(page, 'Move stock', 'Replenish a pick face — same temperature/capacity stop');
  await humanClick(page, page.getByRole('button', { name: /Move/ }));
  await expect(page).toHaveURL(/\/move(\?|$)/);
  await scene(page, 'Replenishment move', 'Reserve bay → pick face · Confirm posts a Move to the ledger');
  await humanClick(page, page.getByRole('button', { name: /Confirm move/ }));
  await waitForHub();

  await scene(page, 'Operator shift done', 'Receive → Put away → Pick → Pack → Move — all on the ledger');
  await beat(page, 600);
});
