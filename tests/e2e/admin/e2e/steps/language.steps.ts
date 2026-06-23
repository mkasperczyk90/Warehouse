import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { When, Then } = createBdd(test);

When('the manager switches the language', async ({ app }) => {
  await app.switchLanguage();
});

Then('the navigation shows {string}', async ({ app }, text: string) => {
  await app.expectText(text);
});
