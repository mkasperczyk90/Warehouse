import { expect, type Page } from '@playwright/test';

/** Put-away (terminal-3-putaway · UC-04). */
export class PutAwayPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/putaway');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/putaway(\?|$)/);
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  /** The big proposed-location address (exact isolates it from the "why" line). */
  async expectProposedLocation(address: string) {
    await expect(this.page.getByText(address, { exact: true })).toBeVisible();
  }

  /** A green environment/capacity check row (Invariant #1/#2 made visible). */
  async expectCheck(text: string) {
    await expect(this.page.getByText(text)).toBeVisible();
  }

  async reportLocationFull() {
    await this.page.getByRole('button', { name: /Location full/ }).click();
  }

  async confirm() {
    await this.page.getByRole('button', { name: /Confirm put-away/ }).click();
  }
}
