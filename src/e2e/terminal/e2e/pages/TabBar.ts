import { expect, type Locator, type Page } from '@playwright/test';

/** The persistent bottom tab bar (Tasks / Scan / Look up / More). */
export class TabBar {
  constructor(private readonly page: Page) {}

  /** Tabs are role=tab; role queries exclude the mounted-but-hidden screens. */
  tab(name: string): Locator {
    return this.page.getByRole('tab', { name: new RegExp(name) });
  }

  async open(name: string) {
    await this.tab(name).click();
  }

  async expectDisabled(name: string) {
    await expect(this.tab(name)).toHaveAttribute('aria-disabled', 'true');
  }
}
