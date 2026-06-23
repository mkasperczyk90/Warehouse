import { expect } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the coordinator opens the outbound orders', async ({ outbound }) => {
  await outbound.open();
});

Then('the outbound list is shown', async ({ outbound }) => {
  await outbound.expectShown();
});

When('the coordinator selects order {string}', async ({ outbound }, id: string) => {
  await outbound.selectOrder(id);
});

When('the coordinator splits the order', async ({ outbound }) => {
  await outbound.split();
});

When('the coordinator releases the order to a wave', async ({ outbound }) => {
  await outbound.releaseToWave();
});

When('the coordinator cancels the order', async ({ outbound }) => {
  await outbound.cancelOrder();
});

// --- Create order ----------------------------------------------------------
When('the coordinator starts a new order', async ({ outbound }) => {
  await outbound.openCreate();
});

When('the coordinator enters the customer {string}', async ({ outbound }, customer: string) => {
  await outbound.fillCustomer(customer);
});

When(
  'the coordinator adds an order line with SKU {string} and quantity {int}',
  async ({ outbound }, sku: string, qty: number) => {
    await outbound.fillFirstLine(sku, qty);
  },
);

Then('the create-order button is disabled', async ({ outbound }) => {
  await expect(outbound.createButton()).toBeDisabled();
});

Then('the create-order button is enabled', async ({ outbound }) => {
  await expect(outbound.createButton()).toBeEnabled();
});

When('the coordinator submits the order', async ({ outbound }) => {
  await outbound.submitCreate();
});
