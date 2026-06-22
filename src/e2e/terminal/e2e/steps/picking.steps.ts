import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the operator opens the picking screen', async ({ picking }) => {
  await picking.open();
});

Then('the go-to location {string} is shown', async ({ picking }, address: string) => {
  await picking.expectText(address);
});

Then('the product to pick is {string}', async ({ picking }, product: string) => {
  await picking.expectText(product);
});

Then('the FEFO batch {string} is shown', async ({ picking }, batch: string) => {
  await picking.expectText(batch);
});

Then('the pick cannot be confirmed yet', async ({ picking }) => {
  await picking.expectConfirmGated();
});

Then('the pick can be confirmed', async ({ picking }) => {
  await picking.expectCanConfirm();
});

When('the operator scans the location and the product', async ({ picking }) => {
  await picking.scanLocationThenProduct();
});

When('the operator confirms the pick', async ({ picking }) => {
  await picking.confirm();
});

When('the operator reports a short pick {string}', async ({ picking }, reason: string) => {
  await picking.reportShort(reason);
});
