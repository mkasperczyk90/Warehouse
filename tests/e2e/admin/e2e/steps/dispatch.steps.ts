import { expect } from '@playwright/test';
import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the coordinator opens the dispatch board', async ({ dispatch }) => {
  await dispatch.open();
});

Then('the dispatch board is shown', async ({ dispatch }) => {
  await dispatch.expectShown();
});

Then('there are {int} assign-carrier actions', async ({ dispatch }, n: number) => {
  await expect(dispatch.assignButtons()).toHaveCount(n);
});

Then('there is {int} assign-carrier action', async ({ dispatch }, n: number) => {
  await expect(dispatch.assignButtons()).toHaveCount(n);
});

When(
  'the coordinator assigns a carrier {string} to the first packed shipment',
  async ({ dispatch }, code: string) => {
    await dispatch.openAssign();
    await dispatch.selectCarrier(code);
    await dispatch.submitAssign();
  },
);

When('the coordinator filters the board by carrier {string}', async ({ dispatch }, name: string) => {
  await dispatch.filterByCarrier(name);
});

Then('shipment {string} is shown', async ({ dispatch }, id: string) => {
  await dispatch.expectShipment(id);
});

Then('shipment {string} is not shown', async ({ dispatch }, id: string) => {
  await dispatch.expectShipmentHidden(id);
});
