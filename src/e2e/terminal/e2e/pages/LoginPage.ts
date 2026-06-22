import { expect, type Page } from '@playwright/test';

/** Badge-scan sign-in (terminal-0-login) — the gate before the task stack. */
export class LoginPage {
  private readonly badgePlaceholder = 'Scan badge or type number…';

  constructor(private readonly page: Page) {}

  /** The login screen has no route of its own — it replaces the app at `/`. */
  async open() {
    await this.page.goto('/');
  }

  async expectShown() {
    await expect(this.page.getByText('Sign in', { exact: true })).toBeVisible();
    await expect(this.page.getByPlaceholder(this.badgePlaceholder)).toBeVisible();
  }

  /** A badge reader types the number then sends Enter — emulate that. */
  async scanBadge(badge: string) {
    const field = this.page.getByPlaceholder(this.badgePlaceholder);
    await field.fill(badge);
    await field.press('Enter');
  }

  /** The inline "not recognised" message is an accessibility alert. */
  async expectError() {
    await expect(this.page.getByRole('alert')).toBeVisible();
  }
}
