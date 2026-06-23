import { expect, type Page } from '@playwright/test';

/** Move stock (terminal-5-move · UC-06). */
export class MovePage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/move');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/move(\?|$)/);
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  /** A from/to leg address. */
  async expectLeg(address: string) {
    await expect(this.page.getByText(address, { exact: true })).toBeVisible();
  }

  /** A green environment/capacity check row (same hard invariant as put-away). */
  async expectCheck(text: string) {
    await expect(this.page.getByText(text)).toBeVisible();
  }

  async confirm() {
    await this.page.getByRole('button', { name: /Confirm move/ }).click();
  }

  async transfer() {
    await this.page.getByRole('button', { name: /Inter-warehouse transfer/ }).click();
  }
}
