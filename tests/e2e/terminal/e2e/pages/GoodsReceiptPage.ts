import { expect, type Page } from '@playwright/test';

/** Goods receipt (terminal-2-receive · UC-02). */
export class GoodsReceiptPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/receive');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/receive(\?|$)/);
    // exact: the Scan screen's "Open goods receipt →" would match a substring.
    await expect(this.page.getByText('Goods receipt', { exact: true })).toBeVisible();
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  /** The counted/expected number — exact match isolates the stepper value. */
  async expectCount(value: string) {
    await expect(this.page.getByText(value, { exact: true })).toBeVisible();
  }

  async increase() {
    await this.page.getByLabel('increase').click();
  }

  async decrease() {
    await this.page.getByLabel('decrease').click();
  }

  async confirmLine() {
    await this.page.getByRole('button', { name: /Confirm line/ }).click();
  }

  /** Report a discrepancy → choose a reason in the sheet; the receipt still proceeds. */
  async reportDiscrepancy(reason: string | RegExp) {
    await this.page.getByRole('button', { name: /Report discrepancy/ }).click();
    await this.page.getByRole('button', { name: reason }).click();
  }
}
