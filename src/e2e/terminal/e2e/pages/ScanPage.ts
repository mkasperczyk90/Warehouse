import { expect, type Locator, type Page } from '@playwright/test';

/** Scan tab (terminal Scan) — the universal scan dispatcher. */
export class ScanPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/scan');
  }

  /** A hardware scanner types the payload then sends Enter — emulate that. */
  async scan(code: string) {
    const field = this.page.getByPlaceholder('Pull the trigger or type a code…');
    await field.fill(code);
    await field.press('Enter');
  }

  /** The resolved kind also appears in the seeded history → target the newest. */
  async expectResult(title: string) {
    await expect(this.page.getByText(title).first()).toBeVisible();
  }

  /** The single action on the result card — its label always ends with "→". */
  private actionButton(): Locator {
    return this.page.getByRole('button', { name: /→/ });
  }

  async expectActionOffered(namePattern: RegExp) {
    await expect(this.page.getByRole('button', { name: namePattern })).toBeVisible();
  }

  async expectNoAction() {
    await expect(this.page.getByText('Nothing to do — try a different code.')).toBeVisible();
    await expect(this.actionButton()).toHaveCount(0);
  }

  async takeAction() {
    await this.actionButton().click();
  }
}
