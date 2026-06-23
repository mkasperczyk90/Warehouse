import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { When, Then } = createBdd(test);

Then('the interface shows {string}', async ({ language }, text: string) => {
  await language.expectText(text);
});

When('the operator switches the language', async ({ language }) => {
  await language.toggle();
});
