import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { When, Then } = createBdd(test);

When('the manager searches stock for {string}', async ({ stock }, term: string) => {
  await stock.search(term);
});

When('the manager filters stock by {string}', async ({ stock }, pill: string) => {
  await stock.filterBy(pill);
});

When('the manager opens the stock row {string}', async ({ stock }, text: string) => {
  await stock.openRow(text);
});

Then('the stock row {string} is shown', async ({ stock }, text: string) => {
  await stock.expectRowShown(text);
});

Then('the stock row {string} is not shown', async ({ stock }, text: string) => {
  await stock.expectRowHidden(text);
});
