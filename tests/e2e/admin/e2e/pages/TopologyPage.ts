import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Warehouse topology (admin-7-topology · UC-14). A tree of warehouses → rooms
 * → locations on the left, the selected room's detail (locations table) on the
 * right. The first room is selected by default.
 */
export class TopologyPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/topology');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/topology(\?|$)/);
    await expect(this.page.getByText('Cold room 1 — WH-01')).toBeVisible();
  }

  async selectRoom(label: string) {
    await this.page.getByText(label, { exact: true }).click();
  }
  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  // --- Add a location to the selected room ---------------------------------
  async addLocation(address: string) {
    await this.page.getByRole('button', { name: 'Add location' }).click();
    await this.page.getByLabel('Address').fill(address);
    await this.page.getByRole('button', { name: 'Create location' }).click();
  }

  // --- Save the room (no-op persistence, but exercises the save path) ------
  async saveRoom() {
    await this.page.getByRole('button', { name: 'Save room' }).click();
  }
  async expectRoomSaved() {
    await expect(this.page.getByText('Room saved ✓')).toBeVisible();
  }
}
