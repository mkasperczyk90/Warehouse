import { expect, type Locator, type Page } from '@playwright/test';

/** Task hub (terminal-1-hub) — the operator's landing screen. */
export class TaskHubPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/');
  }

  /** A task pile is a role=button whose accessible name contains its title. */
  pile(name: string): Locator {
    return this.page.getByRole('button', { name: new RegExp(name) });
  }

  async openPile(name: string) {
    await this.pile(name).click();
  }

  /** The identity bar doubles as the sign-out control (its a11y label). */
  async signOut() {
    await this.page.getByRole('button', { name: 'Sign out' }).click();
  }

  scanField(): Locator {
    return this.page.getByPlaceholder('Scan a barcode to start…');
  }

  /** Robust "are we on the hub?" check — role query excludes hidden screens. */
  async expectShown() {
    await expect(this.pile('Receive')).toBeVisible();
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  /** The big count on a pile — scoped to the tile so it can't match elsewhere. */
  async expectPileCount(name: string, count: string) {
    await expect(this.pile(name).getByText(count, { exact: true })).toBeVisible();
  }
}
