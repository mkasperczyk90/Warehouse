import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Quality holds (admin-8-qc · UC-03 Quality inspection). Quarantined batches
 * awaiting a release/reject decision; every decision needs an audited reason.
 */
export class QualityPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/quality');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/quality(\?|$)/);
    await expect(this.page.getByText('Batches in quarantine')).toBeVisible();
  }

  /** Scope decision buttons to the batch's own table row (implicit role=row). */
  private batchRow(text: string): Locator {
    return this.page.getByRole('row', { name: new RegExp(text) });
  }

  async release(batch: string) {
    await this.batchRow(batch).getByRole('button', { name: 'Release' }).click();
  }
  async reject(batch: string) {
    await this.batchRow(batch).getByRole('button', { name: 'Reject' }).click();
  }

  async selectReason(value: string) {
    await this.page.getByLabel('Reason').selectOption(value);
  }

  confirmReleaseButton(): Locator {
    return this.page.getByRole('button', { name: 'Confirm release' });
  }
  confirmRejectButton(): Locator {
    return this.page.getByRole('button', { name: 'Confirm reject' });
  }
  async confirmRelease() {
    await this.confirmReleaseButton().click();
  }

  async expectBatchGone(text: string) {
    await expect(this.page.getByText(text)).toHaveCount(0);
  }
}
