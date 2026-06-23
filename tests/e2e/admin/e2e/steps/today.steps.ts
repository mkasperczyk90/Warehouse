import { expect } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { When, Then } = createBdd(test);

Then('the card {string} is shown', async ({ today }, name: string) => {
  await expect(today.card(name)).toBeVisible();
});

When('the manager opens the {string} card', async ({ today }, name: string) => {
  await today.openCard(name);
});
