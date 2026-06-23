import { expect, type Locator, type Page } from '@playwright/test';

/** Stock view (admin-1-stock · UC-05) — KPIs, quick filters and the stock table. */
export class StockPage {
  private readonly searchPlaceholder = 'Search SKU, name, batch or location…';

  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/stock');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/stock(\?|$)/);
    // The "On hand" KPI is the screen's landmark before the table loads.
    await expect(this.page.getByText('On hand').first()).toBeVisible();
  }

  searchField(): Locator {
    return this.page.getByPlaceholder(this.searchPlaceholder);
  }

  async search(term: string) {
    await this.searchField().fill(term);
  }

  /** Quick-filter pills are buttons labelled by their text (All / Cold room / …). */
  async filterBy(pill: string) {
    await this.page.getByRole('button', { name: pill, exact: true }).click();
  }

  /** Table rows are role=button (clickable drill-down); name matches a cell substring. */
  row(text: string): Locator {
    return this.page.getByRole('button', { name: new RegExp(text) });
  }

  async openRow(text: string) {
    await this.row(text).click();
  }

  async expectRowShown(text: string) {
    await expect(this.row(text)).toBeVisible();
  }

  async expectRowHidden(text: string) {
    await expect(this.row(text)).toHaveCount(0);
  }
}
