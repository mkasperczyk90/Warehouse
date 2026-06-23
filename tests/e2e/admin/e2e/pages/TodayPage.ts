import { expect, type Locator, type Page } from '@playwright/test';

/** Today landing (admin-10) — the desk's work-queue worklist, reached at `/`. */
export class TodayPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/today');
  }

  /** `/` redirects here; assert the URL and the page heading. */
  async expectShown() {
    await expect(this.page).toHaveURL(/\/today(\?|$)/);
    await expect(
      this.page.getByRole('heading', { name: 'What needs you now' }),
    ).toBeVisible();
  }

  /** Each KPI card is a role=button whose accessible name contains its label. */
  card(name: string): Locator {
    return this.page.getByRole('button', { name: new RegExp(name) });
  }

  async openCard(name: string) {
    await this.card(name).click();
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }
}
