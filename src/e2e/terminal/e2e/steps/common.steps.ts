import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, Then } = createBdd(test);

Given('the operator opens the terminal', async ({ taskHub }) => {
  await taskHub.open();
});

Then('the goods receipt screen opens', async ({ goodsReceipt }) => {
  await goodsReceipt.expectShown();
});

Then('the task hub is shown again', async ({ taskHub }) => {
  await taskHub.expectShown();
});
