import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Manual stock adjustment (admin-9-adjustment · UC-08). Seeds from a draft,
 * computes the delta, refuses a negative result, and gates the irreversible
 * ledger post behind a confirm dialog.
 */
export class AdjustmentPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/adjustment');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/adjustment(\?|$)/);
    await expect(
      this.page.getByRole('heading', { name: 'Manual stock adjustment' }),
    ).toBeVisible();
  }

  async setQuantity(value: string) {
    await this.page.getByLabel('New counted quantity').fill(value);
  }
  async selectReason(value: string) {
    await this.page.getByLabel('Reason').selectOption(value);
  }
  async expectDelta(value: string) {
    await expect(this.page.getByText(value, { exact: true }).first()).toBeVisible();
  }

  async post() {
    await this.page.getByRole('button', { name: 'Post adjustment to ledger' }).click();
  }
  async expectConfirmDialog() {
    await expect(this.page.getByText('Post adjustment to the ledger?')).toBeVisible();
  }
  async confirmPost() {
    await this.page.getByRole('button', { name: 'Confirm — post to ledger' }).click();
  }

  postedBanner(): Locator {
    return this.page.getByText('Posted to the ledger ✓');
  }
  async expectPosted() {
    await expect(this.postedBanner()).toBeVisible();
  }
  async expectNotPosted() {
    await expect(this.postedBanner()).toHaveCount(0);
  }
  async expectBelowZeroError() {
    await expect(this.page.getByText(/can never go below zero/)).toBeVisible();
  }
}
