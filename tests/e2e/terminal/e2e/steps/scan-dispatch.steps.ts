import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the operator opens the Scan tab', async ({ scan }) => {
  await scan.open();
});

When('the operator scans {string}', async ({ scan }, code: string) => {
  await scan.scan(code);
});

Then('the result is recognised as a/an {string}', async ({ scan }, title: string) => {
  await scan.expectResult(title);
});

Then('the result is {string}', async ({ scan }, title: string) => {
  await scan.expectResult(title);
});

Then('an action to open goods receipt is offered', async ({ scan }) => {
  await scan.expectActionOffered(/Open goods receipt/);
});

Then('an action to look up stock is offered', async ({ scan }) => {
  await scan.expectActionOffered(/Look up stock & ATP/);
});

Then('no action is offered', async ({ scan }) => {
  await scan.expectNoAction();
});

When('the operator takes that action', async ({ scan }) => {
  await scan.takeAction();
});

Then('the look up screen opens', async ({ lookup }) => {
  await lookup.expectShown();
});
