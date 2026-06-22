import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the manager opens the movements ledger', async ({ movements }) => {
  await movements.open();
});

Then('the movements ledger is shown', async ({ movements }) => {
  await movements.expectShown();
});

Then('the movement reference {string} is shown', async ({ movements }, reference: string) => {
  await movements.expectReference(reference);
});

Then(
  'the movement reference {string} is no longer in the ledger',
  async ({ movements }, reference: string) => {
    await movements.expectReferenceHidden(reference);
  },
);

When('the manager filters movements by type {string}', async ({ movements }, label: string) => {
  await movements.filterByType(label);
});

When('the manager searches movements for {string}', async ({ movements }, term: string) => {
  await movements.search(term);
});
