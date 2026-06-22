import { expect, type Page } from '@playwright/test';

/** Look up tab — read-only inquiry over stock, locations and batches. */
export class LookupPage {
  private readonly searchPlaceholder = 'Search SKU, name, location, batch…';

  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/lookup');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/lookup(\?|$)/);
    await expect(this.page.getByPlaceholder(this.searchPlaceholder)).toBeVisible();
  }

  async search(term: string) {
    await this.page.getByPlaceholder(this.searchPlaceholder).fill(term);
  }

  async filterBy(kind: string) {
    await this.page.getByRole('button', { name: kind }).click();
  }

  /** e.g. "9 results" — exact so "1 result" ≠ "1 results". */
  async expectResultCount(label: string) {
    await expect(this.page.getByText(label, { exact: true })).toBeVisible();
  }

  async expectRowShown(text: string) {
    await expect(this.page.getByText(text, { exact: true })).toBeVisible();
  }

  async expectRowHidden(text: string) {
    await expect(this.page.getByText(text, { exact: true })).toHaveCount(0);
  }

  async expectBadge(label: string) {
    await expect(this.page.getByText(label)).toBeVisible();
  }
}
