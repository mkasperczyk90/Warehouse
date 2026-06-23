import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the manager opens the profile screen', async ({ profile }) => {
  await profile.open();
  await profile.expectShown();
});

When('the manager opens their profile from the user menu', async ({ app }) => {
  await app.openProfileFromMenu();
});

When('the manager sets the phone to {string}', async ({ profile }, value: string) => {
  await profile.setPhone(value);
});

When('the manager saves the profile', async ({ profile }) => {
  await profile.save();
});

Then('the profile shows it was saved', async ({ profile }) => {
  await profile.expectSaved();
});
