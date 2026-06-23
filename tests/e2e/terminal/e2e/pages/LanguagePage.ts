import { expect, type Page } from '@playwright/test';

/** The PL/EN language control in the bar (and its effect on the UI copy). */
export class LanguagePage {
  constructor(private readonly page: Page) {}

  /** The control's accessible name is itself localized, so match either language. */
  async toggle() {
    await this.page.getByRole('button', { name: /Switch language|Zmień język/ }).first().click();
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }
}
