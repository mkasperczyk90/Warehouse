import { expect } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the manager opens the stocktakes', async ({ stocktake }) => {
  await stocktake.openList();
});

Given('the manager opens the stocktake review {string}', async ({ stocktake }, id: string) => {
  await stocktake.openReview(id);
});

Then('the stocktake list is shown', async ({ stocktake }) => {
  await stocktake.expectListShown();
});

When('the manager starts a count', async ({ stocktake }) => {
  await stocktake.openStartDialog();
});

Then('the start-count dialog is shown', async ({ stocktake }) => {
  await stocktake.expectStartDialog();
});

Then('the approve-differences button is enabled', async ({ stocktake }) => {
  await expect(stocktake.approveButton()).toBeEnabled();
});

Then('the approve-differences button is disabled', async ({ stocktake }) => {
  await expect(stocktake.approveButton()).toBeDisabled();
});

When('the manager selects the difference at {string}', async ({ stocktake }, location: string) => {
  await stocktake.selectRow(location);
});

When(
  'the manager sets the reason at {string} to {string}',
  async ({ stocktake }, location: string, reason: string) => {
    await stocktake.setRowReason(location, reason);
  },
);

When('the manager approves the differences', async ({ stocktake }) => {
  await stocktake.approve();
});

Then('the differences are posted to the ledger', async ({ stocktake }) => {
  await expect(stocktake.approvedButton()).toBeVisible();
});
