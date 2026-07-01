import { createBdd } from 'playwright-bdd';

import { test } from '../../fixtures';

const { Given, When, Then } = createBdd(test);

Given('the manager opens the topology', async ({ topology }) => {
  await topology.open();
});

Then('the topology is shown', async ({ topology }) => {
  await topology.expectShown();
});

When('the manager selects the room {string}', async ({ topology }, label: string) => {
  await topology.selectRoom(label);
});

When('the manager adds the location {string}', async ({ topology }, address: string) => {
  await topology.addLocation(address);
});

When('the manager saves the room', async ({ topology }) => {
  await topology.saveRoom();
});

Then('the room is saved', async ({ topology }) => {
  await topology.expectRoomSaved();
});
