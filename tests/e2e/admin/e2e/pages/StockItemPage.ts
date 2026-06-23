import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Stock item drill-down (admin-1-stock · UC-05 view, UC-06 move). Owns the
 * move dialog (which enforces the environment-compatibility invariant) and the
 * block-to-quarantine dialog (which requires a reason).
 */
export class StockItemPage {
  constructor(private readonly page: Page) {}

  async open(id: string) {
    await this.page.goto(`/stock/${id}`);
  }

  async expectShown(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  // --- Move (UC-06) — environment invariant --------------------------------
  async openMove() {
    await this.page.getByRole('button', { name: 'Move', exact: true }).click();
  }
  async selectTarget(address: string) {
    await this.page.getByLabel('Target location').selectOption(address);
  }
  confirmMoveButton(): Locator {
    return this.page.getByRole('button', { name: 'Confirm move' });
  }
  async expectIncompatible() {
    await expect(this.page.getByText(/Incompatible/)).toBeVisible();
  }

  // --- Block → quarantine (UC-03 entry) ------------------------------------
  async openBlock() {
    await this.page.getByRole('button', { name: 'Block', exact: true }).click();
  }
  async selectBlockReason(value: string) {
    await this.page.getByLabel('Reason').selectOption(value);
  }
  blockConfirmButton(): Locator {
    return this.page.getByRole('button', { name: 'Block → quarantine' });
  }
  async confirmBlock() {
    await this.blockConfirmButton().click();
  }
}
