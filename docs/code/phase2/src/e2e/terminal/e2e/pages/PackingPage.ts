import { expect, type Page } from '@playwright/test';

/** Packing (terminal-6-pack · UC-11). */
export class PackingPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/pack');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/pack(\?|$)/);
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  /** The active package id, e.g. "PKG 1 · carton M". */
  async expectActivePackage(label: string) {
    await expect(this.page.getByText(label)).toBeVisible();
  }

  async addAnotherPackage() {
    await this.page.getByRole('button', { name: /Add another package/ }).click();
  }

  async closePackage() {
    await this.page.getByRole('button', { name: /Close package/ }).click();
  }
}
