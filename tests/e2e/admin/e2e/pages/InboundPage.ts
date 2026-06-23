import { expect, type Locator, type Page } from '@playwright/test';

/**
 * Inbound / ASN (admin-2-asn · UC-01 Announce delivery, UC-02 Receive).
 * A list of announced deliveries on the left, the selected ASN's detail
 * (fields + lines) on the right. The first ASN is selected by default.
 */
export class InboundPage {
  constructor(private readonly page: Page) {}

  async open() {
    await this.page.goto('/inbound');
  }

  async expectShown() {
    await expect(this.page).toHaveURL(/\/inbound(\?|$)/);
    await expect(this.page.getByText('Announced deliveries')).toBeVisible();
  }

  /** ASN list entries are buttons whose accessible name contains the ASN id. */
  async selectAsn(id: string) {
    await this.page.getByRole('button', { name: new RegExp(id) }).click();
  }

  async expectText(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible();
  }

  // --- Create ASN (UC-01 step 1) -------------------------------------------
  async openCreate() {
    await this.page.getByRole('button', { name: /New ASN/ }).click();
  }
  async fillSupplier(value: string) {
    await this.page.getByLabel('Supplier').fill(value);
  }
  async fillFirstLine(sku: string, qty: number) {
    await this.page.getByLabel('SKU 1').fill(sku);
    await this.page.getByLabel('Qty 1').fill(String(qty));
  }
  createButton(): Locator {
    return this.page.getByRole('button', { name: 'Create ASN' });
  }
  async submitCreate() {
    await this.createButton().click();
  }

  // --- Assign dock slot (UC-01 step 3) -------------------------------------
  async openAssignDock() {
    await this.page.getByRole('button', { name: /Assign dock slot/ }).click();
  }
  async selectDock(dock: string) {
    // exact: the dialog's own aria-label "Assign dock slot" also contains "dock".
    await this.page.getByLabel('Dock', { exact: true }).selectOption(dock);
  }
  async fillWindow(window: string) {
    await this.page.getByLabel('Time window').fill(window);
  }
  async submitDock() {
    await this.page.getByRole('button', { name: 'Assign slot' }).click();
  }

  // --- Mark arrived (lifecycle: Announced → Arrived) -----------------------
  arriveButton(): Locator {
    return this.page.getByRole('button', { name: 'Mark arrived' });
  }
  async markArrived() {
    await this.arriveButton().click();
  }

  // --- View receiving (UC-02) ----------------------------------------------
  async viewReceiving() {
    await this.page.getByRole('button', { name: /View receiving/ }).click();
  }
}
