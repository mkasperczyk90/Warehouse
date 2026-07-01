import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the manager opens the stock adjustment', async ({ adjustment }) => {
  await adjustment.open();
});

Then('the stock adjustment is shown', async ({ adjustment }) => {
  await adjustment.expectShown();
});

When('the manager sets the counted quantity to {string}', async ({ adjustment }, value: string) => {
  await adjustment.setQuantity(value);
});

Then('the delta {string} is shown', async ({ adjustment }, value: string) => {
  await adjustment.expectDelta(value);
});

When('the manager picks the adjustment reason {string}', async ({ adjustment }, reason: string) => {
  await adjustment.selectReason(reason);
});

When('the manager posts the adjustment', async ({ adjustment }) => {
  await adjustment.post();
});

Then('the below-zero error is shown', async ({ adjustment }) => {
  await adjustment.expectBelowZeroError();
});

Then('the adjustment is not posted', async ({ adjustment }) => {
  await adjustment.expectNotPosted();
});

Then('the confirm-post dialog is shown', async ({ adjustment }) => {
  await adjustment.expectConfirmDialog();
});

When('the manager confirms the post', async ({ adjustment }) => {
  await adjustment.confirmPost();
});

Then('the adjustment is posted to the ledger', async ({ adjustment }) => {
  await adjustment.expectPosted();
});
