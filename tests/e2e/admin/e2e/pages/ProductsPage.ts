import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Products (admin-4-product · UC-13). The catalogue (search + category pills,
 * rows drilling into a read-only detail) plus the two write surfaces the app
 * actually ships: a **Define product** modal on the catalogue (validates the SKU
 * and the temperature range client-side) and a **Rename** modal on the detail.
 */
export class ProductsPage {
  private readonly catalogSearch = 'Search name or SKU…';

  constructor(private readonly page: Page) {}

  async openCatalog() {
    await this.page.goto('/products');
  }

  async expectCatalogShown() {
    await expect(this.page).toHaveURL(/\/products(\?|$)/);
    await expect(this.page.getByRole('heading', { name: 'Products' })).toBeVisible();
  }

  async search(term: string) {
    await this.page.getByPlaceholder(this.catalogSearch).fill(term);
  }
  async filterByCategory(label: string) {
    await this.page.getByRole('button', { name: label, exact: true }).click();
  }

  private row(text: string): Locator {
    const escaped = text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    return this.page.getByRole('button', { name: new RegExp(escaped) });
  }
  async expectRowShown(text: string) {
    await expect(this.row(text)).toBeVisible();
  }
  async expectRowHidden(text: string) {
    await expect(this.row(text)).toHaveCount(0);
  }

  /** Front door to a product's read-only detail — deep-link by SKU. */
  async openDetail(sku: string) {
    await this.page.goto(`/products/${sku}`);
  }

  // --- Read-only detail + Rename modal -------------------------------------
  async expectDetailName(name: string) {
    await expect(this.page.getByRole('heading', { name })).toBeVisible();
  }
  async rename(newName: string) {
    await this.page.getByRole('button', { name: 'Rename', exact: true }).click();
    await this.page.getByLabel('New name').fill(newName);
    await this.page.getByRole('button', { name: 'Save', exact: true }).click();
  }

  // --- Define (create) modal -----------------------------------------------
  async openNew() {
    await this.page.getByRole('button', { name: /Define product/ }).click();
  }
  async expectCreateShown() {
    await expect(this.page.getByText('Define a product')).toBeVisible();
  }
  async fillValidSkuAndName(sku = 'NEW-SKU-1', name = 'Test product') {
    // `exact` — the top-bar and catalogue search boxes also mention "SKU".
    await this.page.getByLabel('SKU', { exact: true }).fill(sku);
    await this.page.getByLabel('Name', { exact: true }).fill(name);
  }
  async setColdChainRange(min: string, max: string) {
    await this.page.getByLabel('Storage').selectOption('ColdChain');
    await this.page.getByLabel(/Temp\. min/).fill(min);
    await this.page.getByLabel(/Temp\. max/).fill(max);
  }
  async createProduct() {
    await this.page.getByRole('button', { name: 'Create product' }).click();
  }
  async expectTempError() {
    await expect(this.page.getByText('Max temperature must be ≥ min')).toBeVisible();
  }
  async expectSkuError() {
    await expect(this.page.getByText(/SKU: 2–32 chars/)).toBeVisible();
  }
}
