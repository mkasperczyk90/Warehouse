import { expect } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the manager opens the stock item {string}', async ({ stockItem }, id: string) => {
  await stockItem.open(id);
});

// --- Move ------------------------------------------------------------------
When('the manager opens the move dialog', async ({ stockItem }) => {
  await stockItem.openMove();
});

When('the manager picks the target location {string}', async ({ stockItem }, address: string) => {
  await stockItem.selectTarget(address);
});

Then('the move is flagged incompatible', async ({ stockItem }) => {
  await stockItem.expectIncompatible();
});

Then('the confirm-move button is disabled', async ({ stockItem }) => {
  await expect(stockItem.confirmMoveButton()).toBeDisabled();
});

Then('the confirm-move button is enabled', async ({ stockItem }) => {
  await expect(stockItem.confirmMoveButton()).toBeEnabled();
});

// --- Block -----------------------------------------------------------------
When('the manager opens the block dialog', async ({ stockItem }) => {
  await stockItem.openBlock();
});

When('the manager picks the block reason {string}', async ({ stockItem }, reason: string) => {
  await stockItem.selectBlockReason(reason);
});

When('the manager confirms the block', async ({ stockItem }) => {
  await stockItem.confirmBlock();
});

Then('the block-confirm button is disabled', async ({ stockItem }) => {
  await expect(stockItem.blockConfirmButton()).toBeDisabled();
});
