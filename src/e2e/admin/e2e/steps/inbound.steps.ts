import { expect } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the coordinator opens the inbound deliveries', async ({ inbound }) => {
  await inbound.open();
});

Then('the inbound list is shown', async ({ inbound }) => {
  await inbound.expectShown();
});

When('the coordinator selects ASN {string}', async ({ inbound }, id: string) => {
  await inbound.selectAsn(id);
});

// --- Create ASN ------------------------------------------------------------
When('the coordinator starts a new ASN', async ({ inbound }) => {
  await inbound.openCreate();
});

When('the coordinator enters the supplier {string}', async ({ inbound }, supplier: string) => {
  await inbound.fillSupplier(supplier);
});

When(
  'the coordinator adds a line with SKU {string} and quantity {int}',
  async ({ inbound }, sku: string, qty: number) => {
    await inbound.fillFirstLine(sku, qty);
  },
);

Then('the create-ASN button is disabled', async ({ inbound }) => {
  await expect(inbound.createButton()).toBeDisabled();
});

Then('the create-ASN button is enabled', async ({ inbound }) => {
  await expect(inbound.createButton()).toBeEnabled();
});

When('the coordinator submits the ASN', async ({ inbound }) => {
  await inbound.submitCreate();
});

// --- Dock slot -------------------------------------------------------------
When(
  'the coordinator assigns dock {string} with window {string}',
  async ({ inbound }, dock: string, window: string) => {
    await inbound.openAssignDock();
    await inbound.selectDock(dock);
    await inbound.fillWindow(window);
    await inbound.submitDock();
  },
);

// --- Arrive ----------------------------------------------------------------
When('the coordinator marks the ASN as arrived', async ({ inbound }) => {
  await inbound.markArrived();
});

Then('the mark-arrived button is gone', async ({ inbound }) => {
  await expect(inbound.arriveButton()).toHaveCount(0);
});

// --- Resolve unknown SKU ---------------------------------------------------
When(
  'the coordinator resolves the flagged line to SKU {string} product {string}',
  async ({ inbound }, sku: string, product: string) => {
    await inbound.resolveFirstFlaggedLine();
    await inbound.fillResolution(sku, product, true);
    await inbound.submitResolution();
  },
);

// --- Receiving -------------------------------------------------------------
When('the coordinator opens the receiving view', async ({ inbound }) => {
  await inbound.viewReceiving();
});
