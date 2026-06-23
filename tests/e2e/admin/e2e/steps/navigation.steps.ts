import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { When, Then } = createBdd(test);

When('the manager opens the {string} section', async ({ app }, name: string) => {
  await app.navigateTo(name);
});

Then('the Today worklist is shown', async ({ today }) => {
  await today.expectShown();
});

Then('the Stock view is shown', async ({ stock }) => {
  await stock.expectShown();
});

Then('the {string} section is disabled', async ({ app }, name: string) => {
  await app.expectNavDisabled(name);
});
