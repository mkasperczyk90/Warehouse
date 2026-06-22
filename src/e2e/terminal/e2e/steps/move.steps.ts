import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the operator opens the move screen', async ({ move }) => {
  await move.open();
});

Then('the move leg {string} is shown', async ({ move }, address: string) => {
  await move.expectLeg(address);
});

Then('the move product {string} is shown', async ({ move }, product: string) => {
  await move.expectText(product);
});

Then('the move check {string} passes', async ({ move }, text: string) => {
  await move.expectCheck(text);
});

When('the operator confirms the move', async ({ move }) => {
  await move.confirm();
});

When('the operator issues an inter-warehouse transfer', async ({ move }) => {
  await move.transfer();
});
