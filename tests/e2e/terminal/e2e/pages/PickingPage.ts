import { expect, type Locator, type Page } from '@playwright/test';

/** Picking (terminal-4-pick · UC-10). */
export class PickingPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/pick');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/pick(\?|$)/);
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  private scanField(): Locator {
    // The pick screen has a single scan input; its placeholder changes per step.
    return this.page.getByRole('textbox').first();
  }

  /** Emulate a scanner: type any payload then Enter (the pick only counts the step). */
  private async scan(payload: string) {
    const field = this.scanField();
    await field.fill(payload);
    await field.press('Enter');
  }

  /** The scan IS the commit — location then product, in order. */
  async scanLocationThenProduct() {
    await this.scan('WH01-A2-A09-R1-S2');
    await this.scan('4006381333931');
  }

  /** Before both scans land, the primary action reads "Scan to confirm" and is gated. */
  async expectConfirmGated() {
    await expect(this.page.getByRole('button', { name: /Scan to confirm/ })).toBeVisible();
  }

  async expectCanConfirm() {
    await expect(this.page.getByRole('button', { name: /Confirm pick/ })).toBeVisible();
  }

  async confirm() {
    await this.page.getByRole('button', { name: /Confirm pick/ }).click();
  }

  /** Short pick → pick a reason in the sheet → the system replans. */
  async reportShort(reason: string | RegExp) {
    await this.page.getByRole('button', { name: /Short pick/ }).click();
    await this.page.getByRole('button', { name: reason }).click();
  }
}
