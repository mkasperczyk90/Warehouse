import { expect } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, Then } = createBdd(test);

Given('the manager opens the admin panel', async ({ app }) => {
  await app.open('/');
});

Given('the manager opens the Stock view', async ({ stock }) => {
  await stock.open();
});

/** Generic landmark assertion — first visible match of the copy. */
Then('{string} is shown', async ({ app }, text: string) => {
  await app.expectText(text);
});

/** Generic absence assertion — the copy is gone from the DOM. */
Then('{string} is no longer shown', async ({ page }, text: string) => {
  await expect(page.getByText(text)).toHaveCount(0);
});

Then('the heading {string} is shown', async ({ page }, name: string) => {
  await expect(page.getByRole('heading', { name })).toBeVisible();
});

/** The router appends nothing fancy, but allow a trailing query for safety. */
Then('the URL is {string}', async ({ page }, path: string) => {
  const escaped = path.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  await page.waitForURL(new RegExp(`${escaped}(\\?|$)`));
});
