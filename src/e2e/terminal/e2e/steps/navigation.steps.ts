import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { When, Then } = createBdd(test);

When('the operator opens the {string} tab', async ({ tabBar }, name: string) => {
  await tabBar.open(name);
});

Then('the Scan screen is shown', async ({ page }) => {
  await page.waitForURL(/\/scan(\?|$)/);
  await page.getByPlaceholder('Pull the trigger or type a code…').waitFor();
});

Then('the Look up screen is shown', async ({ lookup }) => {
  await lookup.expectShown();
});

Then('the {string} tab is disabled', async ({ tabBar }, name: string) => {
  await tabBar.expectDisabled(name);
});
