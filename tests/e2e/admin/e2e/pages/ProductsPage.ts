import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Products (admin-4-product · UC-13). The catalogue (search + category pills,
 * rows drilling into an editor) and the create/edit form (which validates the
 * SKU and the temperature range).
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
  async openProduct(text: string) {
    await this.row(text).click();
  }
  async openNew() {
    await this.page.getByRole('button', { name: /New product/ }).click();
  }
  /** Front door to the editor — deep-link to a product by SKU. */
  async openEdit(sku: string) {
    await this.page.goto(`/products/${sku}`);
  }

  // --- Edit / create form (UC-13) ------------------------------------------
  nameField(): Locator {
    return this.page.getByLabel('Name');
  }
  async expectName(value: string) {
    await expect(this.nameField()).toHaveValue(value);
  }
  async setTempMin(value: string) {
    await this.page.getByLabel('Temperature min').fill(value);
  }
  async save() {
    await this.page.getByRole('button', { name: 'Save product' }).click();
  }
  async expectSaved() {
    await expect(this.page.getByText('Product saved ✓')).toBeVisible();
  }
  async expectTempError() {
    await expect(this.page.getByText(/Max temperature must be ≥ min/)).toBeVisible();
  }
  async expectNotSaved() {
    await expect(this.page.getByText('Product saved ✓')).toHaveCount(0);
  }

  async expectCreateShown() {
    await expect(this.page.getByText('New product').first()).toBeVisible();
  }
  async createProduct() {
    await this.page.getByRole('button', { name: 'Create product' }).click();
  }
  async expectSkuError() {
    await expect(this.page.getByText(/SKU must be at least 8 characters/)).toBeVisible();
  }
}
