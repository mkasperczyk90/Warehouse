import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the terminal is at the sign-in screen', async ({ login }) => {
  await login.open();
  await login.expectShown();
});

When('the operator scans the badge {string}', async ({ login }, badge: string) => {
  await login.scanBadge(badge);
});

When('the operator signs out', async ({ taskHub }) => {
  await taskHub.signOut();
});

Then('the hub greets {string}', async ({ taskHub }, name: string) => {
  await taskHub.expectText(name);
});

Then('the sign-in error is shown', async ({ login }) => {
  await login.expectError();
});

Then('the sign-in screen is shown', async ({ login }) => {
  await login.expectShown();
});
