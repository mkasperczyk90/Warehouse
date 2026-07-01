import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Dispatch board (admin-6-dispatch · UC-12). A kanban of shipments across
 * Packed → Assigned → Notice sent → Dispatched; assign a carrier, advance a
 * shipment, or filter the board by carrier.
 */
export class DispatchPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/dispatch');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/dispatch(\?|$)/);
    await expect(this.page.getByText('Packed — awaiting carrier')).toBeVisible();
  }

  assignButtons(): Locator {
    return this.page.getByRole('button', { name: 'Assign carrier' });
  }
  async openAssign() {
    await this.assignButtons().first().click();
  }
  async selectCarrier(code: string) {
    // exact: the dialog's own aria-label "Assign carrier" also contains "Carrier".
    await this.page.getByLabel('Carrier', { exact: true }).selectOption(code);
  }
  submitAssignButton(): Locator {
    return this.page.getByRole('button', { name: 'Assign', exact: true });
  }
  async submitAssign() {
    await this.submitAssignButton().click();
  }

  async sendPickupNotice() {
    await this.page.getByRole('button', { name: /Send pickup notice/ }).first().click();
  }

  async filterByCarrier(name: string) {
    await this.page.getByRole('button', { name, exact: true }).click();
  }

  async expectShipment(id: string) {
    await expect(this.page.getByText(id)).toBeVisible();
  }
  async expectShipmentHidden(id: string) {
    await expect(this.page.getByText(id)).toHaveCount(0);
  }
}
