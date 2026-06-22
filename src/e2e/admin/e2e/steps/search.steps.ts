import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { When, Then } = createBdd(test);

When('the manager searches globally for {string}', async ({ search }, term: string) => {
  await search.search(term);
});

When('the manager opens the search result {string}', async ({ search }, text: string) => {
  await search.openResult(text);
});

Then('the search result {string} is shown', async ({ search }, text: string) => {
  await search.expectResult(text);
});
