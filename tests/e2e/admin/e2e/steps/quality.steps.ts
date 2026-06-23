import { expect } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the inspector opens the quality holds', async ({ quality }) => {
  await quality.open();
});

Then('the quality holds are shown', async ({ quality }) => {
  await quality.expectShown();
});

When('the inspector rejects the batch {string}', async ({ quality }, batch: string) => {
  await quality.reject(batch);
});

When('the inspector releases the batch {string}', async ({ quality }, batch: string) => {
  await quality.release(batch);
});

When('the inspector picks the reason {string}', async ({ quality }, reason: string) => {
  await quality.selectReason(reason);
});

When('the inspector confirms the release', async ({ quality }) => {
  await quality.confirmRelease();
});

Then('the confirm-reject button is disabled', async ({ quality }) => {
  await expect(quality.confirmRejectButton()).toBeDisabled();
});

Then('the confirm-reject button is enabled', async ({ quality }) => {
  await expect(quality.confirmRejectButton()).toBeEnabled();
});

Then('the batch {string} is no longer shown', async ({ quality }, batch: string) => {
  await quality.expectBatchGone(batch);
});
