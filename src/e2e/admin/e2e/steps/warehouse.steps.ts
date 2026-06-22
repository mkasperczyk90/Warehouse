import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { When, Then } = createBdd(test);

When('the manager switches to warehouse {string}', async ({ app }, label: string) => {
  await app.switchWarehouse(label);
});

Then('the active warehouse is {string}', async ({ app }, label: string) => {
  await app.expectWarehouse(label);
});
