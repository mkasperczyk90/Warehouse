import { createBdd } from 'playwright-bdd';

import { expect, test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the operator opens the goods receipt screen', async ({ goodsReceipt }) => {
  await goodsReceipt.open();
});

Then('the ASN context {string} is shown', async ({ goodsReceipt }, context: string) => {
  await goodsReceipt.expectText(context);
});

Then('the dock {string} is shown', async ({ goodsReceipt }, dock: string) => {
  await goodsReceipt.expectText(dock);
});

Then('the product {string} is shown', async ({ goodsReceipt }, product: string) => {
  await goodsReceipt.expectText(product);
});

Then(
  'the counted quantity starts at the expected {string}',
  async ({ goodsReceipt }, value: string) => {
    await goodsReceipt.expectCount(value);
  },
);

Then('the counted quantity is {string}', async ({ goodsReceipt }, value: string) => {
  await goodsReceipt.expectCount(value);
});

When('the operator increases the count once', async ({ goodsReceipt }) => {
  await goodsReceipt.increase();
});

When('the operator decreases the count twice', async ({ goodsReceipt }) => {
  await goodsReceipt.decrease();
  await goodsReceipt.decrease();
});

Given('the operator started from the task hub', async ({ page, taskHub }) => {
  await taskHub.open();
  await taskHub.openPile('Receive');
  await expect(page).toHaveURL(/\/receive(\?|$)/);
});

When('the operator confirms the line', async ({ goodsReceipt }) => {
  await goodsReceipt.confirmLine();
});

When('the operator reports a discrepancy {string}', async ({ goodsReceipt }, reason: string) => {
  await goodsReceipt.reportDiscrepancy(reason);
});
