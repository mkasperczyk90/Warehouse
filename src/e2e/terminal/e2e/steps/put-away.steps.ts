import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the operator opens the put-away screen', async ({ putAway }) => {
  await putAway.open();
});

Then('the proposed location is {string}', async ({ putAway }, address: string) => {
  await putAway.expectProposedLocation(address);
});

Then('the pallet {string} is shown', async ({ putAway }, text: string) => {
  await putAway.expectText(text);
});

Then('the put-away check {string} passes', async ({ putAway }, text: string) => {
  await putAway.expectCheck(text);
});

When('the operator reports the location full', async ({ putAway }) => {
  await putAway.reportLocationFull();
});

When('the operator confirms the put-away', async ({ putAway }) => {
  await putAway.confirm();
});
