import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Stocktake (admin-3-stocktake · UC-07). The list screen schedules/starts a
 * blind count; the review screen (`/stocktake/$id`) approves the differences
 * to the ledger — every selected difference needs a reason before approval.
 */
export class StocktakePage {
  constructor(private readonly page: Page) {}

  async openList() {
    await this.page.goto('/stocktake');
  }
  async openReview(id: string) {
    await this.page.goto(`/stocktake/${id}`);
  }

  async expectListShown() {
    await expect(this.page).toHaveURL(/\/stocktake(\?|$)/);
    await expect(this.page.getByRole('heading', { name: 'Stocktakes' })).toBeVisible();
  }

  async openStartDialog() {
    await this.page.getByRole('button', { name: /Start count/ }).click();
  }
  async expectStartDialog() {
    await expect(this.page.getByText('Start a stocktake')).toBeVisible();
    await expect(this.page.getByRole('button', { name: 'Start blind count' })).toBeVisible();
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  // --- Review / approval ----------------------------------------------------
  approveButton(): Locator {
    return this.page.getByRole('button', { name: /Approve differences/ });
  }
  async approve() {
    await this.approveButton().click();
  }
  approvedButton(): Locator {
    return this.page.getByRole('button', { name: /approved → ledger/ });
  }

  /** A difference row without a pre-filled reason — selecting it blocks approval. */
  async selectRow(location: string) {
    await this.page.getByLabel(`select ${location}`).check();
  }
  async setRowReason(location: string, value: string) {
    await this.page.getByLabel(`reason ${location}`).selectOption(value);
  }

  async recount() {
    await this.page.getByRole('button', { name: 'Recount selected' }).click();
  }
}
