import { expect, type Page } from '@playwright/test';

/**
 * Stock movements (admin · the immutable ledger view, read-only). Lists every
 * movement; filterable by type and free-text search. Stock is a projection of
 * these entries (ADR-0002).
 */
export class MovementsPage {
  private readonly searchPlaceholder = 'Search product, SKU, location or reference…';

  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/movements');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/movements(\?|$)/);
    await expect(this.page.getByRole('heading', { name: 'Stock movements' })).toBeVisible();
  }

  async filterByType(label: string) {
    await this.page.getByRole('button', { name: label, exact: true }).click();
  }
  async search(term: string) {
    await this.page.getByPlaceholder(this.searchPlaceholder).fill(term);
  }

  async expectReference(reference: string) {
    await expect(this.page.getByText(reference)).toBeVisible();
  }
  async expectReferenceHidden(reference: string) {
    await expect(this.page.getByText(reference)).toHaveCount(0);
  }
}
