import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Badge-scan sign-in screen (`features/Auth`). Shown only when no session is
 * cached; `open()` deliberately clears the seeded session so the screen appears.
 */
export class LoginPage {
  constructor(private readonly page: Page) {}

  /** Start anonymous: drop the seeded user before the SPA boots, then load. */
  async open() {
    await this.page.addInitScript(() => window.localStorage.removeItem('wh.currentUser'));
    await this.page.goto('/');
  }

  async expectShown() {
    await expect(this.page.getByRole('heading', { name: /Scan your badge/ })).toBeVisible();
  }

  badgeField(): Locator {
    return this.page.getByRole('textbox');
  }

  /** A badge reader types the id and submits; mimic that with fill + Sign in. */
  async scan(badge: string) {
    await this.badgeField().fill(badge);
    await this.page.getByRole('button', { name: /Sign in/ }).click();
  }

  async expectError() {
    await expect(this.page.getByRole('alert')).toBeVisible();
  }
}
