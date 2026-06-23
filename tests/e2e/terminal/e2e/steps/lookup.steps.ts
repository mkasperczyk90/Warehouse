import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the operator opens the Look up tab', async ({ lookup }) => {
  await lookup.open();
});

Then('{string} are listed', async ({ lookup }, label: string) => {
  await lookup.expectResultCount(label);
});

When('the operator searches for {string}', async ({ lookup }, term: string) => {
  await lookup.search(term);
});

When('the operator filters by {string}', async ({ lookup }, kind: string) => {
  await lookup.filterBy(kind);
});

Then('{string} is shown', async ({ lookup }, text: string) => {
  await lookup.expectRowShown(text);
});

Then('{string} is not shown', async ({ lookup }, text: string) => {
  await lookup.expectRowHidden(text);
});

Then('a {string} status badge is shown', async ({ lookup }, label: string) => {
  await lookup.expectBadge(label);
});
