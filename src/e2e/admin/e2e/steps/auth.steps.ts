import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the desk shows the login screen', async ({ login }) => {
  await login.open();
  await login.expectShown();
});

When('the user scans badge {string}', async ({ login }, badge: string) => {
  await login.scan(badge);
});

Then('the login error is shown', async ({ login }) => {
  await login.expectError();
});

Then('the login screen is still shown', async ({ login }) => {
  await login.expectShown();
});
