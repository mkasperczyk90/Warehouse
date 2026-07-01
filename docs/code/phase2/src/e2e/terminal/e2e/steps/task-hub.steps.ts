import { createBdd } from 'playwright-bdd';

import { expect, test } from '../../fixtures';

const { When, Then } = createBdd(test);

Then(
  'the operator sees their name {string} and site {string}',
  async ({ taskHub }, name: string, site: string) => {
    await taskHub.expectText(name);
    await taskHub.expectText(site);
  },
);

Then('the connectivity status shows {string}', async ({ taskHub }, status: string) => {
  // The badge renders "● Online"; substring match tolerates the dot.
  await taskHub.expectText(status);
});

Then('an always-focused scan field invites a scan', async ({ taskHub }) => {
  await expect(taskHub.scanField()).toBeVisible();
});

Then(
  'the task piles {string}, {string}, {string} and {string} are shown',
  async ({ taskHub }, a: string, b: string, c: string, d: string) => {
    for (const name of [a, b, c, d]) {
      await expect(taskHub.pile(name)).toBeVisible();
    }
  },
);

When('the operator taps the {string} pile', async ({ taskHub }, name: string) => {
  await taskHub.openPile(name);
});

// Shared across features: completing a task drops its pile (counts are stateful).
Then('the {string} pile shows {string}', async ({ taskHub }, name: string, count: string) => {
  await taskHub.expectPileCount(name, count);
});
