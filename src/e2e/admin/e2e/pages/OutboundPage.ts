import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Outbound orders (admin-5-outbound · UC-09). A list of orders + the selected
 * order's lines; the coordinator decides split/hold on a partial order,
 * releases a reserved order to a wave, or cancels (releasing reservations).
 */
export class OutboundPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/outbound');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/outbound(\?|$)/);
    await expect(this.page.getByText('Outbound orders')).toBeVisible();
  }

  async selectOrder(id: string) {
    await this.page.getByRole('button', { name: new RegExp(id) }).click();
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  async split() {
    await this.page.getByRole('button', { name: 'Split' }).click();
  }
  async releaseToWave() {
    await this.page.getByRole('button', { name: 'Release to wave' }).click();
  }
  async cancelOrder() {
    await this.page.getByRole('button', { name: 'Cancel order' }).click();
  }

  // --- Create order (UC-09 step 1) -----------------------------------------
  async openCreate() {
    await this.page.getByRole('button', { name: /New order/ }).click();
  }
  async fillCustomer(value: string) {
    await this.page.getByLabel('Customer').fill(value);
  }
  async fillFirstLine(sku: string, qty: number) {
    await this.page.getByLabel('SKU 1').fill(sku);
    await this.page.getByLabel('Qty 1').fill(String(qty));
  }
  createButton(): Locator {
    return this.page.getByRole('button', { name: 'Create order' });
  }
  async submitCreate() {
    await this.createButton().click();
  }
}
