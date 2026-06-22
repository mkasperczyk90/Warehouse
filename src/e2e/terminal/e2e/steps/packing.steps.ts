import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the operator opens the packing screen', async ({ packing }) => {
  await packing.open();
});

// Also the handoff target when a pick is confirmed (picking.feature).
Then('the packing screen opens', async ({ packing }) => {
  await packing.expectShown();
});

Then('the active package is {string}', async ({ packing }, label: string) => {
  await packing.expectActivePackage(label);
});

Then('the packing detail {string} is shown', async ({ packing }, text: string) => {
  await packing.expectText(text);
});

When('the operator opens another package', async ({ packing }) => {
  await packing.addAnotherPackage();
});

When('the operator closes the package', async ({ packing }) => {
  await packing.closePackage();
});
